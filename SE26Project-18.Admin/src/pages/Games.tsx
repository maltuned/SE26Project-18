import React from 'react';
import { Table, Input, Button, Modal, Form, Upload, message, Typography, Space } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { UploadFile, RcFile } from 'antd/es/upload';
import { ArrowLeftOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { searchGames, updateGame, createGame, deleteGame, uploadImage, imageApi } from '../api';

const { Title } = Typography;

const API_BASE = 'http://localhost:5111';

const resolveImage = (url?: string) => {
  if (!url) return '';
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  return API_BASE + (url.startsWith('/') ? url : '/' + url);
};

const AuthImage: React.FC<{ src: string; width?: number; height?: number }> = ({ src, width = 48, height = 48 }) => {
  const [blobUrl, setBlobUrl] = React.useState<string>('');
  const blobRef = React.useRef<string>('');

  React.useEffect(() => {
    let cancelled = false;
    const resolvedSrc = resolveImage(src);
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
  return <img src={blobUrl} alt="" width={width} height={height} style={{ borderRadius: 4, objectFit: 'cover' }} />;
};

interface Game {
  id: number;
  name?: string;
  company?: string;
  description?: string;
  cover?: string;
  icon?: string;
}

const Games: React.FC = () => {
  const navigate = useNavigate();
  const [data, setData] = React.useState<Game[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [query, setQuery] = React.useState('');
  const [modalOpen, setModalOpen] = React.useState(false);
  const [editingGame, setEditingGame] = React.useState<Game | null>(null);
  const [form] = Form.useForm();
  const [saving, setSaving] = React.useState(false);
  const [coverFile, setCoverFile] = React.useState<UploadFile[]>([]);
  const [iconFile, setIconFile] = React.useState<UploadFile[]>([]);

  const fetchData = async (q: string) => {
    setLoading(true);
    try {
      const res = await searchGames(q);
      setData(res.data || res || []);
    } catch {
      // handled by interceptor
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => fetchData(query);

  const uploadSingle = async (file: RcFile, folder: string, name?: string): Promise<string> => {
    const res = await uploadImage(file, folder, name);
    return res.data || '';
  };

  const openCreateModal = () => {
    setEditingGame(null);
    form.resetFields();
    setCoverFile([]);
    setIconFile([]);
    setModalOpen(true);
  };

  const openEditModal = (game: Game) => {
    setEditingGame(game);
    form.setFieldsValue({
      name: game.name,
      company: game.company,
      description: game.description,
    });
    setCoverFile(
      game.cover ? [{ uid: '-1', name: 'cover', status: 'done', url: game.cover }] : []
    );
    setIconFile(
      game.icon ? [{ uid: '-2', name: 'icon', status: 'done', url: game.icon }] : []
    );
    setModalOpen(true);
  };

  const handleSave = async () => {
    try {
      const values = await form.validateFields();
      setSaving(true);

      const payload = {
        name: values.name,
        company: values.company ?? '',
        description: values.description ?? '',
        cover: editingGame?.cover ?? '',
        icon: editingGame?.icon ?? '',
        tagsId: [],
      };

      let gameId = editingGame?.id ?? 0;

      if (!editingGame) {
        const createRes = await createGame(payload);
        gameId = createRes.data?.id ?? createRes.id;
        message.success('创建成功');
      }

      if (coverFile.length > 0 && coverFile[0].originFileObj) {
        const coverUrl = await uploadSingle(coverFile[0].originFileObj as RcFile, 'covers', String(gameId));
        payload.cover = coverUrl;
      }
      if (iconFile.length > 0 && iconFile[0].originFileObj) {
        const iconUrl = await uploadSingle(iconFile[0].originFileObj as RcFile, 'icons', String(gameId));
        payload.icon = iconUrl;
      }

      if (editingGame) {
        await updateGame(gameId, payload);
        message.success('更新成功');
      } else {
        await updateGame(gameId, payload);
      }

      setModalOpen(false);
      fetchData(query);
    } catch (err: unknown) {
      if ((err as { errorFields?: unknown })?.errorFields) return;
      message.error('操作失败');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = (id: number) => {
    Modal.confirm({
      title: '确认删除',
      content: '确定要删除该游戏吗？此操作不可撤销。',
      okText: '确认删除',
      okType: 'danger',
      cancelText: '取消',
      onOk: async () => {
        try {
          await deleteGame(id);
          message.success('已删除');
          fetchData(query);
        } catch {
          message.error('操作失败');
        }
      },
    });
  };

  const columns: ColumnsType<Game> = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
    { title: '封面', dataIndex: 'cover', key: 'cover', width: 80,
      render: (_, r) => r.cover ? <AuthImage src={r.cover} width={48} height={48} /> : '-',
    },
    {
      title: '图标', dataIndex: 'icon', key: 'icon', width: 80,
      render: (_, r) => r.icon ? <AuthImage src={r.icon} width={32} height={32} /> : '-',
    },
    { title: '名称', dataIndex: 'name', key: 'name' },
    { title: '厂商', dataIndex: 'company', key: 'company' },
    {
      title: '操作',
      key: 'action',
      width: 150,
      render: (_, record) => (
        <Space size={4}>
          <Button size="small" onClick={() => openEditModal(record)}>
            编辑
          </Button>
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
        <Title level={4} style={{ margin: 0 }}>游戏管理</Title>
      </Space>
      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="搜索游戏 ID 或名称"
          allowClear
          value={query}
          onChange={e => setQuery(e.target.value)}
          onSearch={handleSearch}
          enterButton
          style={{ width: 320 }}
        />
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModal}>
          新增游戏
        </Button>
      </Space>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data}
        loading={loading}
        pagination={{ pageSize: 10 }}
      />
      <Modal
        title={editingGame ? '编辑游戏' : '新增游戏'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={handleSave}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="名称" rules={[{ required: true, message: '请输入游戏名称' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="company" label="厂商">
            <Input />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <Input.TextArea rows={4} />
          </Form.Item>
          <Form.Item label="封面">
            <Upload
              listType="picture-card"
              fileList={coverFile}
              maxCount={1}
              beforeUpload={file => {
                setCoverFile([{ uid: '-1', name: file.name, status: 'done', originFileObj: file }]);
                return false;
              }}
              onRemove={() => setCoverFile([])}
            >
              {coverFile.length === 0 && (
                <div>
                  <UploadOutlined />
                  <div style={{ marginTop: 8 }}>上传</div>
                </div>
              )}
            </Upload>
          </Form.Item>
          <Form.Item label="图标">
            <Upload
              listType="picture-card"
              fileList={iconFile}
              maxCount={1}
              beforeUpload={file => {
                setIconFile([{ uid: '-2', name: file.name, status: 'done', originFileObj: file }]);
                return false;
              }}
              onRemove={() => setIconFile([])}
            >
              {iconFile.length === 0 && (
                <div>
                  <UploadOutlined />
                  <div style={{ marginTop: 8 }}>上传</div>
                </div>
              )}
            </Upload>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Games;