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
    const response = await fetch("https://localhost:7210/api/Authentication/Login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ UserName: userIdentifier, Password: password }),
    });

    if (response.ok) {
      const userData = await response.json();
      console.log("API Response:", userData);

      // Example: API returns userData.username and userData.email
      localStorage.setItem("userRole", userData.role);
      localStorage.setItem("userId", userData.userId);
localStorage.setItem("username", userData.username || userIdentifier);
localStorage.setItem("email", userData.email || userIdentifier);


      if (userData.role === 1) {
        navigate("/user");
      } else {
        navigate("/shop");
      }
    } else {
      alert("Invalid Credentials");
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
    <div className='login-page'
      style={{
        backgroundImage: `url(${bgImage})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center'
      }}>
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
              <span onClick={togglePasswordVisibility}>
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </span>
            </div>
          </div>

          <a href='/' className='forgot-password'>Forgot Password?</a>

          <div className='button-group'>
            <button onClick={handleLogin}>Login</button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default Login;
