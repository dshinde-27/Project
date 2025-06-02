import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import '../Layout/layout.css';
import { MdDashboard } from "react-icons/md";
import { IoIosArrowDown, IoIosArrowUp } from "react-icons/io";
import { SiOverleaf } from "react-icons/si";

function Sidebar() {
     const [menuItems, setMenuItems] = useState({});
    const [expandedMenus, setExpandedMenus] = useState({});
    const location = useLocation();
    const navigate = useNavigate();

    const storedRoleId = localStorage.getItem("userRole");
    const roleId = storedRoleId && !isNaN(parseInt(storedRoleId, 10)) ? parseInt(storedRoleId, 10) : null;

    const iconsMap = {
        // Dashboard: <MdDashboard />,
        // 'My Shops': <BiStoreAlt />,
        // Products: <MdStore />,
        // 'User Management': <FaRegUser />,
        // 'User Control': <MdControlCamera/>,
        // Refund :<RiRefund2Line/>,
        // Control :<TfiControlStop/>,
        // 'Shop Management':<BsShop/>,
        // 'Ecommerce Management':<MdOutlineShoppingCart/>,
        // Feedback :<MdOutlineFeedback/>
    };

    useEffect(() => {
        if (!roleId) return;

        axios.get(`https://localhost:7210/api/Permission/GetMenu/${roleId}`)
            .then((response) => {
                if (typeof response.data === "object") {
                    setMenuItems(response.data);
                    const initialExpanded = Object.keys(response.data).reduce((acc, key) => {
                        acc[key] = false;
                        return acc;
                    }, {});
                    setExpandedMenus(initialExpanded);
                }
            })
            .catch((error) => console.error("Error fetching menu items:", error));
    }, [roleId]);

    const toggleSubmenu = (page) => {
        setExpandedMenus((prev) => ({
            ...prev,
            [page]: !prev[page],
        }));
    };

    const normalizeRoute = (name) => name.toLowerCase().replace(/\s+/g, '-');

    return (
        <div className='sidebar-page'>
            <div className='sidebar-container'>
                <div className='sidebar-header'>
                    <SiOverleaf size={28} className='logo-icon' />
                    <div>
                        <h2>Marketa</h2>
                        <h3>{roleId === 1 ? "Admin Panel" : "User Panel"}</h3>
                    </div>
                </div>

                {Object.keys(menuItems).length > 0 ? (
                    Object.entries(menuItems).map(([page, subPages], idx) => {
                        const hasSubmenu = subPages.length > 0;
                        const isExpanded = expandedMenus[page];
                        const isActive = location.pathname.includes(normalizeRoute(page));

                        return (
                            <div key={idx} className='sidebar-section'>
                                <div
                                    className={`sidebar-link ${isActive ? 'active' : ''}`}
                                    onClick={() => hasSubmenu && toggleSubmenu(page)}
                                >
                                    {iconsMap[page] || <MdDashboard />}
                                    {page}
                                    {hasSubmenu && (
                                        <span className='dropdown-arrow'>
                                            {isExpanded ? <IoIosArrowUp /> : <IoIosArrowDown />}
                                        </span>
                                    )}
                                </div>
                                {hasSubmenu && isExpanded && (
                                    <div className='dropdown-submenu'>
                                        {subPages.map((sub, i) => (
                                            <div
                                                key={i}
                                                className={`sidebar-sublink ${location.pathname.includes(normalizeRoute(sub)) ? 'active' : ''}`}
                                                onClick={() => navigate(`/${normalizeRoute(sub)}`)}
                                                style={{ cursor: 'pointer' }}
                                            >
                                                {sub}
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        );
                    })
                ) : (
                    <p className="section-title">No access available</p>
                )}
            </div>
        </div>
    )
}

export default Sidebar