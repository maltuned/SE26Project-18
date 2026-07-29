import React from 'react';
import { Table, Button, Tag, Input, message, Typography, Space, Dropdown } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowLeftOutlined, EllipsisOutlined } from '@ant-design/icons';
import type { MenuProps } from 'antd';
import { useNavigate } from 'react-router-dom';
import { getReviews, updateReviewStatus } from '../api';

const { Title, Text } = Typography;

interface ReviewData {
  id: number;
  reviewer_id: number;
  reviewer_nickname: string;
  reviewer_avatar: string;
  reviewee_id: number;
  reviewee_nickname: string;
  content: string;
  status: string;
  created_at: string;
}

const Reviews: React.FC = () => {
  const navigate = useNavigate();
  const [reviews, setReviews] = React.useState<ReviewData[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [searchId, setSearchId] = React.useState('');

  const fetchData = React.useCallback(async (id?: number) => {
    setLoading(true);
    try {
      const res = await getReviews(id);
      setReviews(res.data || []);
    } catch {
      message.error('加载评价列表失败');
    } finally {
      setLoading(false);
    }
  }, []);

  const handleSearch = (value: string) => {
    setSearchId(value);
    fetchData(value ? Number(value) : undefined);
  };

  React.useEffect(() => {
    React.startTransition(() => { fetchData(); });
  }, []);

  const handleToggleStatus = async (id: number, current: string) => {
    const newStatus = current === '显示' ? '隐藏' : '显示';
    try {
      await updateReviewStatus(id, newStatus);
      message.success(`已${newStatus}`);
      fetchData(searchId ? Number(searchId) : undefined);
    } catch {
      message.error('操作失败');
    }
  };

  const getMenuItems = (record: ReviewData): MenuProps['items'] => [
    {
      key: 'toggle',
      label: record.status === '显示' ? '隐藏' : '显示',
      onClick: () => handleToggleStatus(record.id, record.status),
    },
  ];

  const columns: ColumnsType<ReviewData> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
    {
      title: '评价者',
      key: 'reviewer',
      width: 120,
      render: (_, r) => <Text>{r.reviewer_nickname}</Text>,
    },
    {
      title: '被评价者',
      key: 'reviewee',
      width: 120,
      render: (_, r) => <Text>{r.reviewee_nickname}</Text>,
    },
    {
      title: '评价内容',
      dataIndex: 'content',
      key: 'content',
      ellipsis: true,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 80,
      render: (s: string) => (
        <Tag color={s === '显示' ? 'green' : 'red'}>{s}</Tag>
      ),
    },
    {
      title: '日期',
      dataIndex: 'created_at',
      key: 'created_at',
      width: 110,
    },
    {
      title: '操作',
      key: 'action',
      width: 80,
      render: (_, record) => (
        <Dropdown menu={{ items: getMenuItems(record) }} trigger={['click']}>
          <Button type="text" icon={<EllipsisOutlined />} />
        </Dropdown>
      ),
    },
  ];

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/admin')}>
          返回
        </Button>
        <Title level={4} style={{ margin: 0 }}>评价管理</Title>
      </Space>
      <Input.Search
        placeholder="搜索评价ID"
        allowClear
        value={searchId}
        onChange={e => setSearchId(e.target.value)}
        onSearch={handleSearch}
        enterButton
        style={{ width: 300, marginBottom: 16 }}
      />
      <Table
        rowKey="id"
        columns={columns}
        dataSource={reviews}
        loading={loading}
        pagination={{ pageSize: 10 }}
      />
    </div>
  );
};

export default Reviews;