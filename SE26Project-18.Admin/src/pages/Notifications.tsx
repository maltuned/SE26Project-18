import React from "react";
import {
  Card,
  Form,
  Input,
  Button,
  Select,
  message,
  Typography,
  Space,
} from "antd";
import { SendOutlined, ArrowLeftOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import { sendNotification, searchUsers } from "../api";

const { Title } = Typography;
const { TextArea } = Input;

const Notifications: React.FC = () => {
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = React.useState(false);
  const [users, setUsers] = React.useState<{ value: number; label: string }[]>(
    [],
  );
  const [fetchingUsers, setFetchingUsers] = React.useState(false);
  const [sendMode, setSendMode] = React.useState<"all" | "specific">("all");

  const handleSearchUsers = async (query: string) => {
    if (!query) {
      setUsers([]);
      return;
    }
    setFetchingUsers(true);
    try {
      const res = await searchUsers(query);
      const list = (res.data || res || []) as {
        id: number;
        nickname?: string;
        username?: string;
      }[];
      const options = list.map((u) => ({
        value: u.id,
        label: `${u.nickname || u.username} (ID: ${u.id})`,
      }));
      const numQuery = Number(query);
      if (
        Number.isInteger(numQuery) &&
        !options.some((o) => o.value === numQuery)
      ) {
        options.unshift({ value: numQuery, label: `ID: ${numQuery}` });
      }
      setUsers(options);
    } catch {
      // ignore
    } finally {
      setFetchingUsers(false);
    }
  };

  const handleSubmit = async (values: {
    title: string;
    body: string;
    userId?: number;
  }) => {
    setLoading(true);
    try {
      const payload: { userId?: number; title: string; body: string } = {
        title: values.title,
        body: values.body,
      };
      if (sendMode === "specific" && values.userId) {
        payload.userId = values.userId;
      }
      const res = await sendNotification(payload);
      message.success(res.message || "发送成功");
      form.resetFields();
    } catch {
      message.error("发送失败");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate("/admin")}>
          返回
        </Button>
        <Title level={4} style={{ margin: 0 }}>
          发送通知
        </Title>
      </Space>

      <Card style={{ maxWidth: 600 }}>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          initialValues={{ sendMode: "all" }}
        >
          <Form.Item label="发送范围" name="sendMode">
            <Select
              onChange={(val) => setSendMode(val)}
              options={[
                { value: "all", label: "全部用户" },
                { value: "specific", label: "指定用户" },
              ]}
            />
          </Form.Item>

          {sendMode === "specific" && (
            <Form.Item
              name="userId"
              label="选择用户"
              rules={[{ required: true, message: "请选择用户" }]}
            >
              <Select
                showSearch
                placeholder="输入昵称或用户ID搜索"
                filterOption={false}
                onSearch={handleSearchUsers}
                options={users}
                loading={fetchingUsers}
                notFoundContent={null}
              />
            </Form.Item>
          )}

          <Form.Item
            name="title"
            label="通知标题"
            rules={[{ required: true, message: "请输入标题" }]}
          >
            <Input placeholder="通知标题" maxLength={100} />
          </Form.Item>

          <Form.Item
            name="body"
            label="通知内容"
            rules={[{ required: true, message: "请输入内容" }]}
          >
            <TextArea rows={4} placeholder="通知内容" maxLength={500} />
          </Form.Item>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              icon={<SendOutlined />}
              loading={loading}
            >
              发送通知
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default Notifications;
