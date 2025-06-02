import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Login from './Authentication/Login';
import Register from './Authentication/Register';

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
        <Route path="/register" element={<Register />} />

        {/* Protected Routes with Sidebar */}
        {/* <Route path="/role" element={
          <AuthenticatedLayout><Role /></AuthenticatedLayout>
        } />  */}
      </Routes>
    </Router>
  );
}

export default App;
