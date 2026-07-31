import React from 'react';
import { Table, Button, Tabs, Dropdown, message, Typography, Space, Modal, Descriptions, Tag, List } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { EllipsisOutlined, ArrowLeftOutlined, SearchOutlined } from '@ant-design/icons';
import type { MenuProps } from 'antd';
import { useNavigate } from 'react-router-dom';
import { getReports, handleReport, getReportTarget, imageApi } from '../api';
import { API_BASE } from '../config';
import type { AxiosError } from 'axios';

const { Title } = Typography;

const resolveAvatar = (url?: string) => {
  if (!url) return '';
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  return API_BASE + (url.startsWith('/') ? url : '/' + url);
};

const AuthAvatar: React.FC<{ src: string; size?: number }> = ({ src, size = 64 }) => {
  const [blobUrl, setBlobUrl] = React.useState<string>('');
  const blobRef = React.useRef<string>('');

  React.useEffect(() => {
    let cancelled = false;
    const resolvedSrc = resolveAvatar(src);
    imageApi.get(resolvedSrc, { responseType: 'blob' }).then(res => {
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
  return <img src={blobUrl} alt="" width={size} height={size} style={{ borderRadius: '50%', objectFit: 'cover' }} />;
};

const STATUS_MAP: Record<string, string | undefined> = {
  '全部': undefined,
  '待处理': 'pending',
  '已处理': 'resolved',
  '驳回': 'rejected',
};

interface Report {
  id: number;
  reporter?: { nickname?: string; username?: string };
  targetType?: string;
  targetId?: number;
  violationType?: string;
  content?: string;
  createdAt?: string;
  status?: string;
}

const Reports: React.FC = () => {
  const navigate = useNavigate();
  const [data, setData] = React.useState<Report[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [activeTab, setActiveTab] = React.useState<string>('全部');
  const [detailModal, setDetailModal] = React.useState<{ open: boolean; data: Record<string, unknown> | null }>({ open: false, data: null });
  const [detailLoading, setDetailLoading] = React.useState(false);

  const fetchData = async (status?: string) => {
    setLoading(true);
    try {
      const res = await getReports(status);
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
      await handleReport(id, status);
      message.success('操作成功');
      fetchData(STATUS_MAP[activeTab]);
    } catch {
      message.error('操作失败');
    }
  };

  const handleViewTarget = async (reportId: number) => {
    setDetailLoading(true);
    setDetailModal({ open: true, data: null });
    try {
      const res = await getReportTarget(reportId);
      setDetailModal({ open: true, data: res.data || res });
    } catch (err: unknown) {
      const msg = (err as AxiosError<{ message?: string }>)?.response?.data?.message || '获取目标详情失败';
      message.error(msg);
    } finally {
      setDetailLoading(false);
    }
  };

  const getMenuItems = (record: Report): MenuProps['items'] => [
    {
      key: 'resolved',
      label: '已处理',
      onClick: () => handleAction(record.id, 'resolved'),
    },
    {
      key: 'rejected',
      label: '驳回',
      onClick: () => handleAction(record.id, 'rejected'),
    },
  ];

  const columns: ColumnsType<Report> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
    {
      title: '举报人',
      key: 'reporter',
      render: (_, r) =>
        r.reporter?.nickname || r.reporter?.username || '-',
      width: 100,
    },
    { title: '目标类型', dataIndex: 'targetType', key: 'targetType', width: 90 },
    { title: '目标ID', dataIndex: 'targetId', key: 'targetId', width: 80 },
    { title: '违规类型', dataIndex: 'violationType', key: 'violationType', width: 90 },
    { title: '内容', dataIndex: 'content', key: 'content', ellipsis: true },
    { title: '时间', dataIndex: 'createdAt', key: 'createdAt', width: 170 },
    { title: '状态', dataIndex: 'status', key: 'status', width: 80 },
    {
      title: '操作',
      key: 'action',
      width: 140,
      render: (_, record) => (
        <Space size={0}>
          <Button
            type="link"
            size="small"
            icon={<SearchOutlined />}
            onClick={() => handleViewTarget(record.id)}
          >
            详情
          </Button>
          {record.status === '待处理' && (
            <Dropdown menu={{ items: getMenuItems(record) }} trigger={['click']}>
              <Button type="text" icon={<EllipsisOutlined />} />
            </Dropdown>
          )}
        </Space>
      ),
    },
  ];

  const renderTargetDetail = () => {
    const d = detailModal.data;
    if (!d) return null;
    const targetType = String(d.targetType ?? '');
    const target = d.target as Record<string, unknown> | undefined;
    if (!target) return <Typography.Text type="secondary">暂无数据</Typography.Text>;

    if (targetType === '招募') {
      const game = target.game as Record<string, unknown> | undefined;
      const gameTags = target.gameTags as Array<{ name: string }> | undefined;
      const recruitmentTags = target.recruitmentTags as Array<{ name: string }> | undefined;
      return (
        <Descriptions bordered column={1} size="small">
          <Descriptions.Item label="游戏">{game?.name ? String(game.name) : '-'}</Descriptions.Item>
          <Descriptions.Item label="标题">{String(target.title ?? '-')}</Descriptions.Item>
          <Descriptions.Item label="游戏标签">
            {gameTags?.length ? gameTags.map(t => <Tag key={t.name}>{t.name}</Tag>) : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="招募标签">
            {recruitmentTags?.length ? recruitmentTags.map(t => <Tag key={t.name}>{t.name}</Tag>) : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="详情">{String(target.description ?? '-')}</Descriptions.Item>
        </Descriptions>
      );
    }

    if (targetType === '用户') {
      return (
        <Descriptions bordered column={1} size="small">
          <Descriptions.Item label="头像">
            <AuthAvatar src={String(target.avatar ?? '')} size={64} />
          </Descriptions.Item>
          <Descriptions.Item label="昵称">{String(target.nickname ?? '-')}</Descriptions.Item>
          <Descriptions.Item label="用户名">{String(target.username ?? '-')}</Descriptions.Item>
          <Descriptions.Item label="签名">{String(target.signature ?? '-')}</Descriptions.Item>
        </Descriptions>
      );
    }

    if (targetType === '聊天') {
      const participant = target.participant as Record<string, unknown> | undefined;
      const messages = target.messages as Array<Record<string, unknown>> | undefined;
      return (
        <Descriptions bordered column={1} size="small">
          <Descriptions.Item label="招募标题">{String(target.recruitmentTitle ?? '-')}</Descriptions.Item>
          <Descriptions.Item label="聊天状态">{String(target.chatStatus ?? '-')}</Descriptions.Item>
          <Descriptions.Item label="参与者">
            {participant?.nickname ? `${String(participant.nickname)} (@${String(participant.username ?? '')})` : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="消息列表">
            {messages?.length ? (
              <List
                size="small"
                dataSource={messages}
                renderItem={m => (
                  <List.Item>
                    <Typography.Text strong>{String(m.sender ?? '')}: </Typography.Text>
                    <Typography.Text>{String(m.content ?? '')}</Typography.Text>
                  </List.Item>
                )}
              />
            ) : '暂无消息'}
          </Descriptions.Item>
        </Descriptions>
      );
    }

    const entries = Object.entries(target);
    return (
      <Descriptions bordered column={2} size="small">
        {entries.map(([key, value]) => (
          <Descriptions.Item key={key} label={key}>
            {value === null || value === undefined ? '-' : String(value)}
          </Descriptions.Item>
        ))}
      </Descriptions>
    );
  };

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/admin')}>
          返回
        </Button>
        <Title level={4} style={{ margin: 0 }}>举报管理</Title>
      </Space>
      <Tabs
        activeKey={activeTab}
        onChange={handleTabChange}
        items={['全部', '待处理', '已处理', '驳回'].map(tab => ({
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
      <Modal
        title="举报目标详情"
        open={detailModal.open}
        onCancel={() => setDetailModal({ open: false, data: null })}
        footer={null}
        loading={detailLoading}
        width={640}
      >
        {renderTargetDetail()}
      </Modal>
    </div>
  );
};

export default Reports;