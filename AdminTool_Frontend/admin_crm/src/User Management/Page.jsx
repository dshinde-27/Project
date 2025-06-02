import React, { useState, useEffect } from 'react';
import Sidebar from '../Layout/Sidebar'
import '../Style/userManagement.css';
import { FaPlus, FaSearch, FaEdit, FaTrash, FaUserShield } from "react-icons/fa";
import Navbar from '../Layout/Navbar';
import axios from 'axios';

function Page() {
    const [pages, setPages] = useState([]);
    const [showModal, setShowModal] = useState(false);
    const [isEditMode, setIsEditMode] = useState(false);
    const [editPageId, setEditPageId] = useState(null);
    const [showSearch, setShowSearch] = useState(false);
    const [searchText, setSearchText] = useState('');
    const [newPage, setNewPage] = useState({
        pageName: '',
        subPageName: '',
        status: 'Active',
        description: ''
    });

    const AddPageBaseUrl = 'https://localhost:7210/api/Page/AddPages';
    const GetPageBaseUrl = 'https://localhost:7210/api/Page/GetPages';
    const EditPageBaseUrl = 'https://localhost:7210/api/Page/EditPage';

    useEffect(() => {
        fetchPages();
    }, []);

    const fetchPages = async () => {
        try {
            const response = await axios.get(GetPageBaseUrl);
            setPages(response.data);
        } catch (error) {
            console.error('Error fetching pages:', error);
        }
    };

    const handleAddPage = async () => {
        try {
            await axios.post(AddPageBaseUrl, newPage);
            fetchPages();
            resetModal();
        } catch (error) {
            console.error('Error adding page:', error);
        }
    };

    const handleEditClick = (page) => {
        setNewPage({
            pageName: page.pageName,
            subPageName: page.subPageName,
            status: page.status,
            description: page.description
        });
        setEditPageId(page.id);
        setIsEditMode(true);
        setShowModal(true);
    };

    const handleUpdatePage = async () => {
        try {
            await axios.put(`${EditPageBaseUrl}/${editPageId}`, {
                id: editPageId,
                ...newPage
            });
            fetchPages();
            resetModal();
        } catch (error) {
            console.error('Error updating page:', error);
        }
    };

    const handleDeletePage = async (id) => {
        try {
            await axios.delete(`${GetPageBaseUrl}/${id}`);
            fetchPages();
        } catch (error) {
            console.error('Error deleting page:', error);
        }
    };

    const resetModal = () => {
        setNewPage({ pageName: '', subPageName: '', status: 'Active', description: '' });
        setIsEditMode(false);
        setEditPageId(null);
        setShowModal(false);
    };
    return (
        <div className='userManage-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='userManage-container'>
                    <div className="userManage-header">
                        <h2>Pages</h2>
                        <div className="userManage-actions">
                            <button
                                className="search-icon"
                                onClick={() => setShowSearch(prev => !prev)}
                            >
                                <FaSearch />
                            </button>
                            {showSearch && (
                                <input
                                    type="text"
                                    className="search-box"
                                    placeholder="Search pages..."
                                    value={searchText}
                                    onChange={e => setSearchText(e.target.value)}
                                />
                            )}
                            <button className='add-userManage-btn' onClick={() => setShowModal(true)}>
                                <FaPlus /> Add Pages
                            </button>
                        </div>
                    </div>

                    <div className='userManage-list'>
                        <div className="table-wrapper">
                            <table className='userManage-table'>
                                <thead>
                                    <tr>
                                        <th>Page Name</th>
                                        <th>SubPage Name</th>
                                        <th>Status</th>
                                        <th>Description</th>
                                        <th>Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {pages.map(page => (
                                        <tr key={page.id}>
                                            <td>{page.pageName}</td>
                                            <td>{page.subPageName}</td>
                                            <td>
                                                <span className={`status ${page.status.toLowerCase()}`}>
                                                    {page.status}
                                                </span>
                                            </td>
                                            <td>{page.description}</td>
                                            <td className='action-buttons'>
                                                <FaEdit className='icon edit' title='Edit' onClick={() => handleEditClick(page)} />
                                                <FaTrash className='icon delete' title='Delete' onClick={() => handleDeletePage(page.id)} />
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    {showModal && (
                        <div className='modal-overlay'>
                            <div className='modal-content'>
                                <h3>{isEditMode ? 'Edit Page' : 'Add New Page'}</h3>

                                <label>Page Name</label>
                                <input
                                    type='text'
                                    value={newPage.pageName}
                                    onChange={(e) => setNewPage({ ...newPage, pageName: e.target.value })}
                                />

                                <label>SubPage Name</label>
                                <input
                                    type='text'
                                    value={newPage.subPageName}
                                    onChange={(e) => setNewPage({ ...newPage, subPageName: e.target.value })}
                                />

                                <label>Status</label>
                                <select
                                    value={newPage.status}
                                    onChange={(e) => setNewPage({ ...newPage, status: e.target.value })}
                                >
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>

                                <label>Description</label>
                                <textarea
                                    value={newPage.description}
                                    onChange={(e) => setNewPage({ ...newPage, description: e.target.value })}
                                    rows={3}
                                ></textarea>

                                <div className='modal-actions'>
                                    <button onClick={isEditMode ? handleUpdatePage : handleAddPage}>
                                        {isEditMode ? 'Update' : 'Submit'}
                                    </button>
                                    <button className='cancel-btn' onClick={resetModal}>Cancel</button>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}

export default Page