import React from 'react';
import { Table, Input, Button, Modal, message, Typography, Space, Tag, Tooltip } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { searchRecruitments, closeRecruitment, deleteRecruitment } from '../api';

const { Title } = Typography;

interface Recruitment {
  id: number;
  title?: string;
  game?: { id: number; name?: string };
  gameTags?: { id: number; name: string }[];
  recruitmentTags?: { id: number; name: string }[];
  description?: string;
  publisher?: { id: number; nickname?: string; username?: string };
  status?: string;
}

const Recruitments: React.FC = () => {
  const navigate = useNavigate();
  const [data, setData] = React.useState<Recruitment[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [query, setQuery] = React.useState('');

  const fetchData = async (q: string) => {
    setLoading(true);
    try {
      const id = q ? Number(q) : undefined;
      const res = await searchRecruitments(id);
      setData(res.data || res || []);
    } catch {
      // handled by interceptor
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (value: string) => {
    setQuery(value);
    fetchData(value);
  };

  React.useEffect(() => {
    React.startTransition(() => { fetchData(''); });
  }, []);

  const handleClose = async (id: number) => {
    try {
      await closeRecruitment(id);
      message.success('招募已关闭');
      fetchData(query);
    } catch {
      message.error('操作失败');
    }
  };

  const handleDelete = (id: number) => {
    Modal.confirm({
      title: '确认删除',
      content: '确定要删除该招募吗？此操作不可撤销。',
      okText: '确认删除',
      okType: 'danger',
      cancelText: '取消',
      onOk: async () => {
        try {
          await deleteRecruitment(id);
          message.success('已删除');
          fetchData(query);
        } catch {
          message.error('操作失败');
        }
      },
    });
  };

  const columns: ColumnsType<Recruitment> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
    { title: '游戏', dataIndex: ['game', 'name'], key: 'game', width: 100 },
    { title: '标题', dataIndex: 'title', key: 'title', width: 160 },
    {
      title: '游戏标签',
      key: 'gameTags',
      width: 140,
      render: (_, r) =>
        r.gameTags?.length
          ? r.gameTags.map(t => <Tag key={t.id}>{t.name}</Tag>)
          : '-',
    },
    {
      title: '招募标签',
      key: 'recruitmentTags',
      width: 140,
      render: (_, r) =>
        r.recruitmentTags?.length
          ? r.recruitmentTags.map(t => <Tag key={t.id}>{t.name}</Tag>)
          : '-',
    },
    {
      title: '详情',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
      render: (_, r) => (
        <Tooltip title={r.description}>
          {r.description || '-'}
        </Tooltip>
      ),
    },
    {
      title: '发布者ID',
      dataIndex: ['publisher', 'id'],
      key: 'publisherId',
      width: 90,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 80,
      render: (_, r) => (
        <Tag color={r.status === '招募中' ? 'green' : 'default'}>{r.status}</Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 150,
      render: (_, record) => (
        <Space size={4}>
          {record.status === '招募中' && (
            <Button size="small" onClick={() => handleClose(record.id)}>
              关闭
            </Button>
          )}
          <Button size="small" danger onClick={() => handleDelete(record.id)}>
            删除
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
        <Title level={4} style={{ margin: 0 }}>招募管理</Title>
      </Space>
      <Input.Search
        placeholder="搜索招募ID"
        allowClear
        value={query}
        onChange={e => setQuery(e.target.value)}
        onSearch={handleSearch}
        enterButton
        style={{ width: 320, marginBottom: 16 }}
      />
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data}
        loading={loading}
        pagination={{ pageSize: 10 }}
        scroll={{ x: 1000 }}
      />
    </div>
  );
};

export default Recruitments;