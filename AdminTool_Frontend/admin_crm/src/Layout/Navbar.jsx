import React, { useState } from 'react';
import '../Layout/layout.css';
import { FaUserCircle, FaSignOutAlt, FaCog, FaPlus, FaUser } from 'react-icons/fa';
import { FiSearch } from 'react-icons/fi';
import { useNavigate } from 'react-router-dom';

function Navbar() {
    const [dropdownOpen, setDropdownOpen] = useState(false);

    const storedName = localStorage.getItem("username") || localStorage.getItem("email") || "Guest";
    const user = {
        name: storedName,
        //avatar: "https://i.pravatar.cc/150?img=3",
    };

    const navigate = useNavigate();

    const handleLogout = () => {
        // Clear user data or token (example: localStorage)
        localStorage.removeItem('token'); // or your auth key
        // Navigate to login page
        navigate('/');
    };
    return (
        <div className='navbar-page'>
            <div className='navbar-container'>
                <div className='navbar-search-container'>
                    <FiSearch className="search-icon" />
                    <input type='text' placeholder='Search Route here' />
                </div>

                <div className='user-profile' onClick={() => setDropdownOpen(prev => !prev)}>
                    {/* <img src={user.avatar} alt="avatar" className='user-avatar' /> */}
                    <span className='user-name'>{user.name}</span>

                    {dropdownOpen && (
                        <div className='dropdown-menu'>
                            <div className='dropdown-item'>
                                <FaUser /> Profile
                            </div>
                            <div className='dropdown-item'>
                                <FaPlus /> Create Shop
                            </div>
                            <div className='dropdown-item'>
                                <FaCog /> Settings
                            </div>
                            <div className='dropdown-item' onClick={handleLogout} style={{ cursor: 'pointer' }}>
                                <FaSignOutAlt /> Logout
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default Navbar;
