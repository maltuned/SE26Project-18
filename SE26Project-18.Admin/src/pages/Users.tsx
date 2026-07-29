import React from 'react';
import { Table, Input, Button, Dropdown, Modal, message, Typography, Space, Tag, Avatar, Tooltip } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowLeftOutlined } from '@ant-design/icons';
import type { MenuProps } from 'antd';
import { useNavigate } from 'react-router-dom';
import { searchUsers, updateUser, updateUserStatus, clearUserProfile, imageApi } from '../api';
import { API_BASE } from '../config';

const { Title } = Typography;

const resolveAvatar = (url?: string) => {
  if (!url) return '';
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  return API_BASE + (url.startsWith('/') ? url : '/' + url);
};

const AuthImage: React.FC<{ src: string; width?: number; height?: number }> = ({ src, width = 40, height = 40 }) => {
  const [blobUrl, setBlobUrl] = React.useState<string>('');
  const blobRef = React.useRef<string>('');

  React.useEffect(() => {
    let cancelled = false;
    imageApi.get(src, { responseType: 'blob' }).then(res => {
      if (!cancelled) {
        const url = URL.createObjectURL(res.data);
        blobRef.current = url;
        setBlobUrl(url);
      }
    }).catch(() => {});
    return () => {
      cancelled = true;
      if (blobRef.current) URL.revokeObjectURL(blobRef.current);
    };
  }, [src]);

  if (!blobUrl) return null;
  return <img src={blobUrl} alt="" width={width} height={height} style={{ borderRadius: 4, objectFit: 'cover' }} />;
};

interface User {
  id: number;
  uid?: string;
  username?: string;
  nickname?: string;
  avatar?: string;
  signature?: string;
  gender?: string;
  status?: string;
}

const Users: React.FC = () => {
  const navigate = useNavigate();
  const [data, setData] = React.useState<User[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [query, setQuery] = React.useState('');

  const fetchData = async (q: string) => {
    setLoading(true);
    try {
      const res = await searchUsers(q);
      setData(res.data || res || []);
    } catch {
      // handled by interceptor
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => fetchData(query);

  const handleResetField = (id: number, field: string, label: string) => {
    Modal.confirm({
      title: `重置${label}`,
      content: `确定要重置该用户的${label}吗？`,
      okText: '确认',
      cancelText: '取消',
      onOk: async () => {
        try {
          await updateUser(id, { [field]: '' });
          message.success(`已重置${label}`);
          fetchData(query);
        } catch {
          message.error('操作失败');
        }
      },
    });
  };

  const handleStatusChange = async (id: number, status: string, label: string) => {
    try {
      await updateUserStatus(id, status);
      message.success(`已${label}`);
      fetchData(query);
    } catch {
      message.error('操作失败');
    }
  };

  const handleClearProfile = (id: number) => {
    Modal.confirm({
      title: '确认清空',
      content: '确定要清空该用户的全部个人信息（昵称、头像、签名等）吗？此操作不可撤销。',
      okText: '确认清空',
      okType: 'danger',
      cancelText: '取消',
      onOk: async () => {
        try {
          await clearUserProfile(id);
          message.success('已清空用户信息');
          fetchData(query);
        } catch {
          message.error('操作失败');
        }
      },
    });
  };

  const getStatusItems = (record: User): MenuProps['items'] => [
    {
      key: '正常',
      label: '正常',
      disabled: record.status === '正常',
      onClick: () => handleStatusChange(record.id, '正常', '解封'),
    },
    {
      key: '封禁',
      label: '封禁',
      danger: true,
      disabled: record.status === '封禁',
      onClick: () => handleStatusChange(record.id, '封禁', '封禁'),
    },
  ];

  const getResetItems = (record: User): MenuProps['items'] => [
    {
      key: 'nickname',
      label: '重置昵称',
      onClick: () => handleResetField(record.id, 'nickname', '昵称'),
    },
    {
      key: 'signature',
      label: '重置签名',
      onClick: () => handleResetField(record.id, 'signature', '签名'),
    },
    {
      key: 'avatar',
      label: '重置头像',
      onClick: () => handleResetField(record.id, 'avatar', '头像'),
    },
  ];

  const columns: ColumnsType<User> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
    {
      title: '头像',
      dataIndex: 'avatar',
      key: 'avatar',
      width: 70,
      render: (_, r) => {
        const src = resolveAvatar(r.avatar);
        return src ? (
          <AuthImage src={src} />
        ) : (
          <Avatar size={40}>{r.nickname?.[0]}</Avatar>
        );
      },
    },
    { title: 'UID', dataIndex: 'uid', key: 'uid', width: 100 },
    { title: '用户名', dataIndex: 'username', key: 'username', width: 120 },
    {
      title: '昵称',
      dataIndex: 'nickname',
      key: 'nickname',
      width: 120,
      render: (_, r) => r.nickname || <Typography.Text type="secondary">未设置</Typography.Text>,
    },
    {
      title: '签名',
      dataIndex: 'signature',
      key: 'signature',
      ellipsis: true,
      render: (_, r) => (
        <Tooltip title={r.signature}>
          {r.signature || <Typography.Text type="secondary">未设置</Typography.Text>}
        </Tooltip>
      ),
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 80,
      render: (_, r) => (
        <Tag color={r.status === '封禁' ? 'red' : 'green'}>{r.status}</Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 260,
      render: (_, record) => (
        <Space size={4}>
          <Dropdown menu={{ items: getResetItems(record) }} trigger={['click']}>
            <Button size="small">重置信息</Button>
          </Dropdown>
          <Dropdown menu={{ items: getStatusItems(record) }} trigger={['click']}>
            <Button size="small">变更状态</Button>
          </Dropdown>
          <Button
            size="small"
            danger
            onClick={() => handleClearProfile(record.id)}
          >
            清空
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/admin')}>
          返回
        </Button>
        <Title level={4} style={{ margin: 0 }}>用户管理</Title>
      </Space>
      <Input.Search
        placeholder="搜索用户 ID、用户名或昵称"
        allowClear
        value={query}
        onChange={e => setQuery(e.target.value)}
        onSearch={handleSearch}
        enterButton
        style={{ width: 360, marginBottom: 16 }}
      />
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data}
        loading={loading}
        pagination={{ pageSize: 10 }}
        scroll={{ x: 900 }}
      />
    </div>
  );
};

export default Users;