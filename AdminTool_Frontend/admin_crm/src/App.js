import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import Login from './Authentication/Login';
import ForgetPassword from './Authentication/ForgetPassword';
import User from './User Management/User';
import Page from './User Management/Page';
import Role from './User Management/Role';
import Country from './User Management/Country';
import State from './User Management/State';
import City from './User Management/City';
import Category from './Product Management/Category';
import SubCategory from './Product Management/SubCategory';
import Attribute from './Product Management/Attribute';
import Inbox from './Email/Inbox';
import OpenMail from './Email/OpenMail';
import Chat from './Chat/Chat';
import ChatList from './Chat/ChatList';

// Helper to check auth
const isAuthenticated = () => !!localStorage.getItem("username");

// Save original path for redirect
const ProtectedRoute = ({ children }) => {
  const location = useLocation();

  if (!isAuthenticated()) {
    localStorage.setItem("redirectAfterLogin", location.pathname + location.search);
    return <Navigate to="/" replace />;
  }

  return children;
};

// Layout wrapper (optional, can contain Sidebar/Navbar if needed)
const AuthenticatedLayout = ({ children }) => (
  <div className="app-layout">
    <div className="main-content">{children}</div>
  </div>
);

function App() {
  return (
    <Router>
      <Routes>
        {/* Public Routes */}
        <Route path="/" element={<Login />} />
        <Route path="/forgetPassword" element={<ForgetPassword />} />

        {/* Protected Routes */}
        <Route path="/user" element={
          <ProtectedRoute><AuthenticatedLayout><User /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/role" element={
          <ProtectedRoute><AuthenticatedLayout><Role /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/page" element={
          <ProtectedRoute><AuthenticatedLayout><Page /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/country" element={
          <ProtectedRoute><AuthenticatedLayout><Country /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/state" element={
          <ProtectedRoute><AuthenticatedLayout><State /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/city" element={
          <ProtectedRoute><AuthenticatedLayout><City /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/category" element={
          <ProtectedRoute><AuthenticatedLayout><Category /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/subcatgeory" element={
          <ProtectedRoute><AuthenticatedLayout><SubCategory /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/attribute" element={
          <ProtectedRoute><AuthenticatedLayout><Attribute /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/email" element={
          <ProtectedRoute><AuthenticatedLayout><Inbox /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/email/:id" element={
          <ProtectedRoute><AuthenticatedLayout><OpenMail /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/chat" element={
          <ProtectedRoute><AuthenticatedLayout><Chat /></AuthenticatedLayout></ProtectedRoute>
        } />
        <Route path="/chatlist" element={
          <ProtectedRoute><AuthenticatedLayout><ChatList /></AuthenticatedLayout></ProtectedRoute>
        } />
      </Routes>
    </Router>
  );
}

export default App;
