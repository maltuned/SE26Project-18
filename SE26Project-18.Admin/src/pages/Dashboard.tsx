import React from 'react';
import { Card, Row, Col, Typography } from 'antd';
import {
  WarningOutlined,
  MessageOutlined,
  UserOutlined,
  TeamOutlined,
  AppstoreOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { getPendingCount as fetchPendingCount } from '../api';

const { Title } = Typography;

interface PendingCounts {
  pendingReports: number;
  pendingFeedbacks: number;
}

const cards = [
  { key: 'reports', title: '举报管理', icon: <WarningOutlined />, color: '#ff4d4f', path: '/admin/reports' },
  { key: 'feedbacks', title: '反馈管理', icon: <MessageOutlined />, color: '#1890ff', path: '/admin/feedbacks' },
  { key: 'users', title: '用户管理', icon: <UserOutlined />, color: '#52c41a', path: '/admin/users' },
  { key: 'recruitments', title: '招募管理', icon: <TeamOutlined />, color: '#722ed1', path: '/admin/recruitments' },
  { key: 'games', title: '游戏管理', icon: <AppstoreOutlined />, color: '#fa8c16', path: '/admin/games' },
];

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [pending, setPending] = React.useState<PendingCounts>({
    pendingReports: 0,
    pendingFeedbacks: 0,
  });

  React.useEffect(() => {
    React.startTransition(() => {
      fetchPendingCount()
        .then(res => {
          const data = res.data || res;
          setPending({
            pendingReports: data.pendingReports ?? data.pending_reports ?? 0,
            pendingFeedbacks: data.pendingFeedbacks ?? data.pending_feedbacks ?? 0,
          });
        })
        .catch(() => {});
    });
  }, []);

  const getPendingCount = (key: string) => {
    if (key === 'reports') return pending.pendingReports;
    if (key === 'feedbacks') return pending.pendingFeedbacks;
    return 0;
  };

  return (
    <div>
      <Title level={3} style={{ marginBottom: 24 }}>概览</Title>
      <Row gutter={[16, 16]}>
        {cards.map(card => {
          const count = getPendingCount(card.key);
          return (
            <Col xs={24} sm={12} lg={8} xl={6} key={card.key}>
              <Card
                hoverable
                onClick={() => navigate(card.path)}
                style={{ borderLeft: `4px solid ${card.color}` }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
                  <div
                    style={{
                      width: 56,
                      height: 56,
                      borderRadius: 12,
                      backgroundColor: `${card.color}15`,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 28,
                      color: card.color,
                      flexShrink: 0,
                    }}
                  >
                    {card.icon}
                  </div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 16, fontWeight: 600 }}>{card.title}</div>
                    <div style={{ color: '#999', fontSize: 13, marginTop: 2 }}>
                      {card.key === 'reports' || card.key === 'feedbacks'
                        ? `待处理 ${count} 条`
                        : '管理'}
                    </div>
                  </div>
                  {(card.key === 'reports' || card.key === 'feedbacks') && count > 0 && (
                    <span
                      style={{
                        backgroundColor: '#ff4d4f',
                        color: '#fff',
                        fontSize: 14,
                        fontWeight: 600,
                        minWidth: 28,
                        height: 28,
                        borderRadius: 14,
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: '0 6px',
                        flexShrink: 0,
                      }}
                    >
                      {count}
                    </span>
                  )}
                </div>
              </Card>
            </Col>
          );
        })}
      </Row>
    </div>
  );
};

export default Dashboard;