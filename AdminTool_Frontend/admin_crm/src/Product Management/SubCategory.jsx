import React, { useState, useEffect } from 'react';
import Sidebar from '../Layout/Sidebar';
import Navbar from '../Layout/Navbar';
import '../Style/product.css';
import { FaPlus, FaSearch, FaEdit, FaTrash } from "react-icons/fa";
import axios from 'axios';

function SubCategory() {
    const [showModal, setShowModal] = useState(false);
    const [isEditMode, setIsEditMode] = useState(false);
    const [editSubCategoryId, setEditSubCategoryId] = useState(null);
    const [showSearch, setShowSearch] = useState(false);
    const [searchText, setSearchText] = useState('');
    const [subCategories, setSubCategories] = useState([]);
    const [categories, setCategories] = useState([]);
    const [newSubCategory, setNewSubCategory] = useState({
        subCategoryName: '',
        status: 'Active',
        description: '',
        categoryId: ''
    });

    const baseUrl = 'https://localhost:7237/api/Category';

    useEffect(() => {
        fetchSubCategories();
        fetchCategories();
    }, []);

    const fetchSubCategories = async () => {
        try {
            const response = await axios.get(`${baseUrl}/GetSubCategories`);
            setSubCategories(response.data);
        } catch (error) {
            console.error('Error fetching SubCategories:', error);
        }
    };

    const fetchCategories = async () => {
        try {
            const response = await axios.get(`${baseUrl}/GetCategories`);
            setCategories(response.data);
        } catch (error) {
            console.error('Error fetching Categories:', error);
        }
    };

    const handleAddSubCategory = async () => {
        debugger
        try {
            await axios.post(`${baseUrl}/AddSubCategory`, newSubCategory);
            fetchSubCategories();
            resetModal();
        } catch (error) {
            console.error('Error adding subcategory:', error);
        }
    };

    const handleUpdateSubCategory = async () => {
        try {
            await axios.put(`${baseUrl}/EditSubCategory/${editSubCategoryId}`, {
                id: editSubCategoryId,
                ...newSubCategory
            });
            fetchSubCategories();
            resetModal();
        } catch (error) {
            console.error('Error updating subcategory:', error);
        }
    };

    const handleDeleteSubCategory = async (id) => {
        try {
            await axios.delete(`${baseUrl}/DeleteSubCategory/${id}`);
            fetchSubCategories();
        } catch (error) {
            console.error('Error deleting subcategory:', error);
        }
    };

    const resetModal = () => {
        setNewSubCategory({
            subCategoryName: '',
            status: 'Active',
            description: '',
            categoryId: ''
        });
        setIsEditMode(false);
        setEditSubCategoryId(null);
        setShowModal(false);
    };

    const handleEditClick = (subCategory) => {
        setNewSubCategory({
            subCategoryName: subCategory.subCategoryName || '',
            slug: subCategory.slug || '',
            status: subCategory.status || 'Active',
            description: subCategory.description || '',
            categoryId: subCategory.categoryId || ''
        });
        setEditSubCategoryId(subCategory.id);
        setIsEditMode(true);
        setShowModal(true);
    };

    const getCategoryName = (categoryId) => {
        const cat = categories.find(c => c.id === categoryId);
        return cat ? cat.categoryName : '';
    };

    return (
        <div className='category-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='category-container'>
                    <div className="category-header">
                        <h2>SubCategory</h2>
                        <div className="category-actions">
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
                                    placeholder="Search SubCategories..."
                                    value={searchText}
                                    onChange={e => setSearchText(e.target.value)}
                                />
                            )}
                            <button className='add-category-btn' onClick={() => setShowModal(true)}>
                                <FaPlus /> Add SubCategory
                            </button>
                        </div>
                    </div>

                    {/* SubCategory List */}
                    <div className='category-list'>
                            <table className='category-table'>
                                <thead>
                                    <tr>
                                        <th>SubCategory</th>
                                        <th>Category</th>
                                        <th>Status</th>
                                        <th>Description</th>
                                        <th>Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {subCategories
                                        .filter(sc =>
                                            sc.subCategoryName?.toLowerCase().includes(searchText.toLowerCase())
                                        )
                                        .map(subCategory => (
                                            <tr key={subCategory.id}>
                                                <td>{subCategory.subCategoryName}</td>
                                                <td>{getCategoryName(subCategory.categoryId)}</td>
                                                <td>
                                                    <span className={`status ${subCategory.status.toLowerCase()}`}>
                                                        {subCategory.status}
                                                    </span>
                                                </td>
                                                <td>{subCategory.description}</td>
                                                <td className='action-buttons'>
                                                    <FaEdit className='icon edit' title='Edit' onClick={() => handleEditClick(subCategory)} />
                                                    <FaTrash className='icon delete' title='Delete' onClick={() => handleDeleteSubCategory(subCategory.id)} />
                                                </td>
                                            </tr>
                                        ))}
                                </tbody>
                            </table>
                    </div>

                    {/* Modal */}
                    {showModal && (
                        <div className='modal-overlay-category'>
                            <div className='modal-content-category'>
                                <h3>{isEditMode ? 'Edit SubCategory' : 'Add New SubCategory'}</h3>

                                <label>SubCategory Name</label>
                                <input
                                    type='text'
                                    value={newSubCategory.subCategoryName}
                                    onChange={(e) =>
                                        setNewSubCategory({ ...newSubCategory, subCategoryName: e.target.value })
                                    }
                                />

                                <label>Category</label>
                                <select
                                    value={newSubCategory.categoryId}
                                    onChange={(e) =>
                                        setNewSubCategory({ ...newSubCategory, categoryId: e.target.value })
                                    }
                                >
                                    <option value="">Select Category</option>
                                    {categories.map(category => (
                                        <option key={category.id} value={category.id}>
                                            {category.categoryName}
                                        </option>
                                    ))}
                                </select>

                                <label>Status</label>
                                <select
                                    value={newSubCategory.status}
                                    onChange={(e) =>
                                        setNewSubCategory({ ...newSubCategory, status: e.target.value })
                                    }
                                >
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>

                                <label>Description</label>
                                <textarea
                                    value={newSubCategory.description}
                                    onChange={(e) =>
                                        setNewSubCategory({ ...newSubCategory, description: e.target.value })
                                    }
                                    rows={3}
                                ></textarea>

                                <div className='modal-actions'>
                                    <button onClick={isEditMode ? handleUpdateSubCategory : handleAddSubCategory}>
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
    );
}

export default SubCategory;
