import React from 'react';
import { Table, Button, Tabs, Dropdown, message, Typography, Space } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { EllipsisOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import type { MenuProps } from 'antd';
import { useNavigate } from 'react-router-dom';
import { getFeedbacks, handleFeedback } from '../api';

const { Title } = Typography;

const STATUS_MAP: Record<string, string | undefined> = {
  '全部': undefined,
  '待处理': 'pending',
  '已处理': 'resolved',
};

interface Feedback {
  id: number;
  user?: { nickname?: string; username?: string };
  type?: string;
  content?: string;
  createdAt?: string;
  status?: string;
}

const Feedbacks: React.FC = () => {
  const navigate = useNavigate();
  const [data, setData] = React.useState<Feedback[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [activeTab, setActiveTab] = React.useState<string>('全部');

  const fetchData = async (status?: string) => {
    setLoading(true);
    try {
      const res = await getFeedbacks(status);
      setData(res.data || res || []);
    } catch {
      message.error('获取数据失败');
    } finally {
      setLoading(false);
    }
  };

  React.useEffect(() => {
    React.startTransition(() => {
      fetchData(STATUS_MAP[activeTab]);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleTabChange = (tab: string) => {
    setActiveTab(tab);
    fetchData(STATUS_MAP[tab]);
  };

  const handleAction = async (id: number, status: string) => {
    try {
      await handleFeedback(id, status);
      message.success('操作成功');
      fetchData(STATUS_MAP[activeTab]);
    } catch {
      message.error('操作失败');
    }
  };

  const getMenuItems = (record: Feedback): MenuProps['items'] => [
    {
      key: 'resolved',
      label: '已处理',
      onClick: () => handleAction(record.id, 'resolved'),
    },
  ];

  const columns: ColumnsType<Feedback> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
    {
      title: '用户',
      key: 'user',
      render: (_, r) =>
        r.user?.nickname || r.user?.username || '-',
    },
    { title: '类型', dataIndex: 'type', key: 'type' },
    { title: '内容', dataIndex: 'content', key: 'content', ellipsis: true },
    { title: '时间', dataIndex: 'createdAt', key: 'createdAt' },
    { title: '状态', dataIndex: 'status', key: 'status', width: 80 },
    {
      title: '操作',
      key: 'action',
      width: 80,
      render: (_, record) => {
        if (record.status === '待处理') {
          return (
            <Dropdown menu={{ items: getMenuItems(record) }} trigger={['click']}>
              <Button type="text" icon={<EllipsisOutlined />} />
            </Dropdown>
          );
        }
        return null;
      },
    },
  ];

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/admin')}>
          返回
        </Button>
        <Title level={4} style={{ margin: 0 }}>反馈管理</Title>
      </Space>
      <Tabs
        activeKey={activeTab}
        onChange={handleTabChange}
        items={['全部', '待处理', '已处理'].map(tab => ({
          key: tab,
          label: tab,
        }))}
      />
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data}
        loading={loading}
        pagination={{ pageSize: 10 }}
      />
    </div>
  );
};

export default Feedbacks;