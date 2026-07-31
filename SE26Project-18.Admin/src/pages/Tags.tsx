import React from 'react';
import { Table, Button, Modal, Input, message, Typography, Space, Tabs, Popconfirm } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowLeftOutlined, PlusOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import {
  getGameTags, createGameTag, updateGameTag, deleteGameTag,
  getRecruitmentTags, createRecruitmentTag, updateRecruitmentTag, deleteRecruitmentTag,
} from '../api';

const { Title } = Typography;

interface TagItem {
  id: number;
  name: string;
}

const Tags: React.FC = () => {
  const navigate = useNavigate();
  const [gameTags, setGameTags] = React.useState<TagItem[]>([]);
  const [recruitmentTags, setRecruitmentTags] = React.useState<TagItem[]>([]);
  const [modalOpen, setModalOpen] = React.useState(false);
  const [editingTag, setEditingTag] = React.useState<TagItem | null>(null);
  const [tagName, setTagName] = React.useState('');
  const [activeTab, setActiveTab] = React.useState('game');
  const [refreshKey, setRefreshKey] = React.useState(0);

  const loadData = React.useCallback(() => {
    React.startTransition(() => {
      getGameTags().then(res => setGameTags(res.data || [])).catch(() => {});
      getRecruitmentTags().then(res => setRecruitmentTags(res.data || [])).catch(() => {});
    });
  }, []);

  React.useEffect(() => {
    loadData();
  }, [loadData, refreshKey]);

  const refresh = () => setRefreshKey(k => k + 1);

  const openCreate = () => {
    setEditingTag(null);
    setTagName('');
    setModalOpen(true);
  };

  const openEdit = (tag: TagItem) => {
    setEditingTag(tag);
    setTagName(tag.name);
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!tagName.trim()) return;
    try {
      if (editingTag) {
        if (activeTab === 'game') {
          await updateGameTag(editingTag.id, tagName.trim());
        } else {
          await updateRecruitmentTag(editingTag.id, tagName.trim());
        }
        message.success('标签更新成功');
      } else {
        if (activeTab === 'game') {
          await createGameTag(tagName.trim());
        } else {
          await createRecruitmentTag(tagName.trim());
        }
        message.success('标签创建成功');
      }
      setModalOpen(false);
      refresh();
    } catch { message.error('操作失败'); }
  };

  const handleDelete = async (id: number) => {
    try {
      if (activeTab === 'game') {
        await deleteGameTag(id);
      } else {
        await deleteRecruitmentTag(id);
      }
      message.success('删除成功');
      refresh();
    } catch { message.error('删除失败'); }
  };

  const columns: ColumnsType<TagItem> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 100 },
    { title: '名称', dataIndex: 'name', key: 'name' },
    {
      title: '操作', key: 'action', width: 200,
      render: (_, record) => (
        <Space>
          <Button type="link" onClick={() => openEdit(record)}>编辑</Button>
          <Popconfirm
            title="确定删除此标签？"
            description="所有关联的游戏和招募中的此标签将被移除"
            onConfirm={() => handleDelete(record.id)}
          >
            <Button type="link" danger>删除</Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const tabItems = [
    {
      key: 'game',
      label: '游戏标签',
      children: (
        <Table
          rowKey="id"
          columns={columns}
          dataSource={gameTags}
          pagination={{ pageSize: 10 }}
        />
      ),
    },
    {
      key: 'recruitment',
      label: '招募标签',
      children: (
        <Table
          rowKey="id"
          columns={columns}
          dataSource={recruitmentTags}
          pagination={{ pageSize: 10 }}
        />
      ),
    },
  ];

  return (
    <div>
      <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/admin')} style={{ marginBottom: 16 }}>
        返回
      </Button>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={4} style={{ margin: 0 }}>标签管理</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>新增标签</Button>
      </div>
      <Tabs activeKey={activeTab} onChange={setActiveTab} items={tabItems} />
      <Modal
        title={editingTag ? '编辑标签' : '新增标签'}
        open={modalOpen}
        onOk={handleSave}
        onCancel={() => setModalOpen(false)}
      >
        <Input
          placeholder="标签名称"
          value={tagName}
          onChange={e => setTagName(e.target.value)}
          style={{ marginTop: 16 }}
        />
      </Modal>
    </div>
  );
};

export default Tags;