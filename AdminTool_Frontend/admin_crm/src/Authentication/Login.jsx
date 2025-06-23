import '../Style/auth.css';
import bgImage from '../Image/bg.jpg';
import { SiOverleaf } from "react-icons/si";
import React, { useState } from 'react';
import { FaEye, FaEyeSlash } from 'react-icons/fa';
import { useNavigate } from 'react-router-dom';

function Login() {
  const [userIdentifier, setUserIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async () => {
    if (!userIdentifier || !password) {
      alert("Please enter both username/email and password.");
      return;
    }

    try {
      debugger
      const response = await fetch("https://localhost:7210/api/Authentication/Login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ UserName: userIdentifier, Password: password }),
      });

      if (response.ok) {
        const userData = await response.json();
        console.log("API Response:", userData);

        localStorage.setItem("userRole", userData.role);
        localStorage.setItem("userId", userData.userId);
        localStorage.setItem("username", userData.username || userIdentifier);
        localStorage.setItem("email", userData.email || userIdentifier);

        const redirectPath = localStorage.getItem("redirectAfterLogin") || "/user";
        localStorage.removeItem("redirectAfterLogin");

        navigate(redirectPath);
      } else {
        alert("Invalid credentials. Please try again.");
      }
    } catch (error) {
      console.error("Login error:", error);
      alert("Something went wrong. Please try again later.");
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword(prev => !prev);
  };

  return (
    <div
      className='login-page'
      style={{
        backgroundImage: `url(${bgImage})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center'
      }}
    >
      <div className='login-container'>
        <div className='login-header'>
          <SiOverleaf />
          <h2>Marketa</h2>
        </div>
        <div className='login-form'>
          <div className='input-group'>
            <label>Username or Email</label>
            <input
              type='text'
              placeholder='Enter Username or Email'
              value={userIdentifier}
              onChange={(e) => setUserIdentifier(e.target.value)}
            />
          </div>

          <div className='input-group'>
            <label>Password</label>
            <div className='password-wrapper'>
              <input
                type={showPassword ? 'text' : 'password'}
                placeholder='Enter Password'
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <span onClick={togglePasswordVisibility} className='password-toggle'>
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </span>
            </div>
          </div>

          <div
            className='forgot-password'
            onClick={() => navigate('/forgetPassword')}
            style={{ cursor: 'pointer' }}
          >
            Forgot Password?
          </div>

          <div className='button-group'>
            <button onClick={handleLogin}>Login</button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default Login;
