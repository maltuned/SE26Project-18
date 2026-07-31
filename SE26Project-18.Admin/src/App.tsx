import React from "react";
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  useNavigate,
  Outlet,
  useLocation,
} from "react-router-dom";
import { Layout, Menu, Button, Typography, theme, Space } from "antd";
import {
  WarningOutlined,
  MessageOutlined,
  UserOutlined,
  TeamOutlined,
  AppstoreOutlined,
  LogoutOutlined,
  DashboardOutlined,
  NotificationOutlined,
  StarOutlined,
  TagsOutlined,
} from "@ant-design/icons";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Reports from "./pages/Reports";
import Feedbacks from "./pages/Feedbacks";
import Notifications from "./pages/Notifications";
import Users from "./pages/Users";
import Recruitments from "./pages/Recruitments";
import Games from "./pages/Games";
import Reviews from "./pages/Reviews";
import Tags from "./pages/Tags";

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

// Protected route wrapper
const ProtectedRoute: React.FC = () => {
  const token = localStorage.getItem("admin_token");
  if (!token) {
    return <Navigate to="/admin/login" replace />;
  }
  return <Outlet />;
};

// Admin layout with sidebar
const AdminLayout: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { token: themeToken } = theme.useToken();

  const menuItems = [
    { key: "/admin", icon: <DashboardOutlined />, label: "概览" },
    { key: "/admin/reports", icon: <WarningOutlined />, label: "举报" },
    { key: "/admin/feedbacks", icon: <MessageOutlined />, label: "反馈" },
    {
      key: "/admin/notifications",
      icon: <NotificationOutlined />,
      label: "通知",
    },
    { key: "/admin/users", icon: <UserOutlined />, label: "用户" },
    { key: "/admin/recruitments", icon: <TeamOutlined />, label: "招募" },
    { key: "/admin/games", icon: <AppstoreOutlined />, label: "游戏" },
    { key: "/admin/reviews", icon: <StarOutlined />, label: "评价" },
    { key: "/admin/tags", icon: <TagsOutlined />, label: "标签" },
  ];

  const handleLogout = () => {
    localStorage.removeItem("admin_token");
    localStorage.removeItem("admin_info");
    navigate("/admin/login");
  };

  const adminInfo = React.useMemo(() => {
    try {
      const info = localStorage.getItem("admin_info");
      return info ? JSON.parse(info) : null;
    } catch {
      return null;
    }
  }, []);

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider
        breakpoint="lg"
        collapsedWidth={0}
        style={{ background: themeToken.colorBgContainer }}
      >
        <div
          style={{
            height: 64,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontWeight: 600,
            fontSize: 16,
            borderBottom: `1px solid ${themeToken.colorBorderSecondary}`,
          }}
        >
          搭子匹配
        </div>
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ borderInlineEnd: "none" }}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            background: themeToken.colorBgContainer,
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "0 24px",
            borderBottom: `1px solid ${themeToken.colorBorderSecondary}`,
          }}
        >
          <Text strong style={{ fontSize: 16 }}>
            搭子匹配 · 管理后台
          </Text>
          <Space>
            {adminInfo && <Text type="secondary">{adminInfo.username}</Text>}
            <Button
              type="text"
              icon={<LogoutOutlined />}
              onClick={handleLogout}
            >
              退出登录
            </Button>
          </Space>
        </Header>
        <Content style={{ margin: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
};

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/admin/login" element={<Login />} />
        <Route path="/admin" element={<ProtectedRoute />}>
          <Route element={<AdminLayout />}>
            <Route index element={<Dashboard />} />
            <Route path="reports" element={<Reports />} />
            <Route path="feedbacks" element={<Feedbacks />} />
            <Route path="notifications" element={<Notifications />} />
            <Route path="users" element={<Users />} />
            <Route path="recruitments" element={<Recruitments />} />
            <Route path="games" element={<Games />} />
            <Route path="reviews" element={<Reviews />} />
          <Route path="tags" element={<Tags />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;