import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
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

function App() {
  const AuthenticatedLayout = ({ children }) => (
    <div className="app-layout">
      {/* <Sidebar /> */}
      <div className="main-content">
        {children}
      </div>
    </div>
  );
  return (
    <Router>
      <Routes>
        {/* Public Routes */}
        <Route path="/" element={<Login />} />
        <Route path="/forgetPassword" element={<ForgetPassword/>}/>

        {/* Protected Routes with Sidebar */}
        <Route path="/user" element={
          <AuthenticatedLayout><User /></AuthenticatedLayout>
        } /> 
        <Route path="/role" element={
          <AuthenticatedLayout><Role /></AuthenticatedLayout>
        } /> 
        <Route path="/page" element={
          <AuthenticatedLayout><Page /></AuthenticatedLayout>
        } /> 
        <Route path="/country" element={
          <AuthenticatedLayout><Country /></AuthenticatedLayout>
        } /> 
        <Route path="/state" element={
          <AuthenticatedLayout><State /></AuthenticatedLayout>
        } /> 
         <Route path="/city" element={
          <AuthenticatedLayout><City /></AuthenticatedLayout>
        } /> 
         <Route path="/category" element={
          <AuthenticatedLayout><Category /></AuthenticatedLayout>
        } /> 
         <Route path="/subcatgeory" element={
          <AuthenticatedLayout><SubCategory /></AuthenticatedLayout>
        } /> 
        <Route path="/attribute" element={
          <AuthenticatedLayout><Attribute /></AuthenticatedLayout>
        } />
      </Routes>
    </Router>
  );
}

export default App;
