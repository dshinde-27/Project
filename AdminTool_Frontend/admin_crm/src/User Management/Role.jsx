import React, { useState, useEffect } from 'react';
import Sidebar from '../Layout/Sidebar';
import Navbar from '../Layout/Navbar';
import '../Style/userManagement.css';
import { FaPlus, FaSearch, FaEdit, FaTrash, FaUserShield } from "react-icons/fa";
import axios from 'axios';
import { useParams, useLocation } from 'react-router-dom';

function Role() {
    const [showSearch, setShowSearch] = useState(false);
    const [searchText, setSearchText] = useState('');
    const [showModal, setShowModal] = useState(false);
    const [roles, setRoles] = useState([]);
    const [showPermissionModal, setShowPermissionModal] = useState(false);
    const [newRole, setNewRole] = useState({ name: '', status: 'Active' });
    const [pages, setPages] = useState([]);
    const [permissions, setPermissions] = useState({});
    const [selectedRoleId, setSelectedRoleId] = useState(null);
    const [selectedRoleName, setSelectedRoleName] = useState('');
    const [loadingPermissions, setLoadingPermissions] = useState(false);

    const [totalRoles, setTotalRoles] = useState(0);
    const [adminPages, setAdminPages] = useState(0);
    const [supervisorPages, setSupervisorPages] = useState(0);
    const [staffPages, setStaffPages] = useState(0);

    const roleApiUrl = 'https://localhost:7210/api/Role';
    const permissionApiUrl = 'https://localhost:7210/api/Permission';
    const pageApiUrl = 'https://localhost:7210/api/Page';

    useEffect(() => {
        const fetchInitialData = async () => {
            await fetchPages();
            await fetchRolesWithPermissionCounts();
        };
        fetchInitialData();
    }, []);

    const fetchPages = async () => {
        try {
            const response = await axios.get(`${pageApiUrl}/GetPages`);
            setPages(response.data);
        } catch (error) {
            console.error("Error fetching pages:", error);
        }
    };

    const fetchPermissions = async (roleId) => {
        try {
            setLoadingPermissions(true);
            const response = await axios.get(`${permissionApiUrl}/GetPermissionsByRole/${roleId}`);
            const perms = {};
            response.data.forEach(p => {
                perms[p.pageId] = p.hasPermission;
            });
            setPermissions(perms);
        } catch (error) {
            console.error("Error fetching permissions:", error);
        } finally {
            setLoadingPermissions(false);
        }
    };

    const fetchRolesWithPermissionCounts = async () => {
        try {
            const roleResponse = await axios.get(`${roleApiUrl}/GetRoles`);
            const allRoles = roleResponse.data;
            setRoles(allRoles);
            setTotalRoles(allRoles.length);

            let adminCount = 0;
            let supervisorCount = 0;
            let staffCount = 0;

            for (const role of allRoles) {
                const permResponse = await axios.get(`${permissionApiUrl}/GetPermissionsByRole/${role.id}`);
                const activePermissions = permResponse.data.filter(p => p.hasPermission);

                switch (role.name.toLowerCase()) {
                    case 'admin':
                        adminCount = activePermissions.length;
                        break;
                    case 'supervisor':
                        supervisorCount = activePermissions.length;
                        break;
                    case 'staff':
                        staffCount = activePermissions.length;
                        break;
                    default:
                        break;
                }
            }

            setAdminPages(adminCount);
            setSupervisorPages(supervisorCount);
            setStaffPages(staffCount);
        } catch (error) {
            console.error("Error fetching roles or permissions:", error);
        }
    };

    const handleAddRole = async () => {
        if (!newRole.name.trim()) {
            alert("Role name cannot be empty.");
            return;
        }

        try {
            const response = await axios.post(`${roleApiUrl}/AddRoles`, newRole, {
                headers: { "Content-Type": "application/json" }
            });
            alert(response.data.message || "Role added successfully!");
            setShowModal(false);
            setNewRole({ name: '', status: 'Active' });
            fetchRolesWithPermissionCounts();
        } catch (error) {
            console.error("Error adding role:", error);
            alert("Failed to add role.");
        }
    };

    const handlePermissionClick = async (roleId) => {
        const role = roles.find(r => r.id === roleId);
        setSelectedRoleId(roleId);
        setSelectedRoleName(role?.name || '');
        setPermissions({});
        setShowPermissionModal(true);
        await fetchPermissions(roleId);
    };

    const handleDeleteRole = async (roleId) => {
        try {
            await axios.delete(`${roleApiUrl}/DeleteRole/${roleId}`);
            alert("Role deleted successfully.");
            fetchRolesWithPermissionCounts();
        } catch (error) {
            console.error("Error deleting role:", error);
            alert("Failed to delete role.");
        }
    };

    const handleCheckboxChange = (pageId) => {
        setPermissions(prev => ({
            ...prev,
            [pageId]: !prev[pageId]
        }));
    };

    const handleSubmit = async () => {
        if (!selectedRoleId) {
            alert("Error: Role ID is missing!");
            return;
        }

        const requestData = pages.map(page => ({
            RoleId: selectedRoleId,
            PageId: page.id,
            HasPermission: !!permissions[page.id]
        }));

        try {
            const response = await axios.post(
                `${permissionApiUrl}/AddOrUpdatePermissions`,
                requestData,
                { headers: { "Content-Type": "application/json" } }
            );
            alert(response.data.message || "Permissions updated successfully!");
            setShowPermissionModal(false);
        } catch (error) {
            console.error("Error updating permissions:", error);
            alert("Failed to update permissions.");
        }
    };

    return (
        <div className='userManage-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='userManage-container'>
                    <div className="userManage-header">
                        <h2>Role</h2>
                        <div className="userManage-actions">
                            <button className="search-icon" onClick={() => setShowSearch(prev => !prev)}>
                                <FaSearch />
                            </button>
                            {showSearch && (
                                <input
                                    type="text"
                                    className="search-box"
                                    placeholder="Search userManages..."
                                    value={searchText}
                                    onChange={e => setSearchText(e.target.value)}
                                />
                            )}
                            <button className='add-userManage-btn' onClick={() => setShowModal(true)}>
                                <FaPlus /> Add Role
                            </button>
                        </div>
                    </div>

                    <div className="user-stats-card">
                        {[
                            { label: 'Total Roles', value: totalRoles },
                            { label: 'Admin', value: adminPages },
                            { label: 'Supervisor', value: supervisorPages },
                            { label: 'Staff', value: staffPages }
                        ].map((stat, i) => (
                            <div className="card" key={i}>
                                <FaUserShield className="card-icon" />
                                <div className="card-info">
                                    <h4>{stat.label}</h4>
                                    <p>{stat.value}</p>
                                </div>
                            </div>
                        ))}
                    </div>

                    <div className='userManage-list'>
                        <table className='userManage-table'>
                            <thead>
                                <tr>
                                    <th>Role Name</th>
                                    <th>Status</th>
                                    <th>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                {roles
                                    .filter(role => role.name.toLowerCase().includes(searchText.toLowerCase()))
                                    .map(role => (
                                        <tr key={role.id}>
                                            <td>{role.name}</td>
                                            <td><span className={`status ${role.status.toLowerCase()}`}>{role.status}</span></td>
                                            <td className='action-buttons'>
                                                <FaEdit className='icon edit' title='Edit' onClick={() => alert("Edit role feature coming soon.")} />
                                                <FaUserShield className='icon permission' title='Permissions' onClick={() => handlePermissionClick(role.id)} />
                                                <FaTrash className='icon delete' title='Delete' onClick={() => handleDeleteRole(role.id)} />
                                            </td>
                                        </tr>
                                    ))}
                            </tbody>
                        </table>
                    </div>

                    {showModal && (
                        <div className='modal-overlay' role="dialog" aria-modal="true">
                            <div className='modal-content'>
                                <h3>Add New Role</h3>
                                <label>Role Name</label>
                                <input
                                    type='text'
                                    value={newRole.name}
                                    onChange={(e) => setNewRole({ ...newRole, name: e.target.value })}
                                />
                                <label>Status</label>
                                <select
                                    value={newRole.status}
                                    onChange={(e) => setNewRole({ ...newRole, status: e.target.value })}
                                >
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>
                                <div className='modal-actions'>
                                    <button onClick={handleAddRole}>Submit</button>
                                    <button className='cancel-btn' onClick={() => setShowModal(false)}>Cancel</button>
                                </div>
                            </div>
                        </div>
                    )}

                    {showPermissionModal && (
                        <div className='modal-overlay-permission' role="dialog" aria-modal="true">
                            <div className='modal-content-permission'>
                                <h3>Assign Permissions for {selectedRoleName}</h3>
                                <table className='userManage-table-permission'>
                                    <thead>
                                        <tr>
                                            <th>Page Name</th>
                                            <th>Sub Page Name</th>
                                            <th>Give Permission</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {loadingPermissions ? (
                                            <tr>
                                                <td colSpan="3">Loading permissions...</td>
                                            </tr>
                                        ) : (
                                            pages.map((page, index) => (
                                                <tr key={index}>
                                                    <td>{page.pageName}</td>
                                                    <td>{page.subPageName || "-"}</td>
                                                    <td>
                                                        <input
                                                            type="checkbox"
                                                            checked={!!permissions[page.id]}
                                                            onChange={() => handleCheckboxChange(page.id)}
                                                        />
                                                    </td>
                                                </tr>
                                            ))
                                        )}
                                    </tbody>
                                </table>
                                <div className='modal-actions-permission'>
                                    <button onClick={handleSubmit}>Save</button>
                                    <button className='cancel-btn' onClick={() => setShowPermissionModal(false)}>Cancel</button>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default Role;
