import React, { useState, useEffect } from 'react';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import '../Style/product.css';
import { FaPlus, FaSearch, FaEdit, FaTrash, FaUserShield } from "react-icons/fa";
import axios from 'axios';
import Select from 'react-select';
import * as FaIcons from 'react-icons/fa';
import * as MdIcons from 'react-icons/md';
import * as Io5Icons from 'react-icons/io5';

function Category() {
    const [showModal, setShowModal] = useState(false);
    const [isEditMode, setIsEditMode] = useState(false);
    const [editCategoryId, setEditCategoryId] = useState(null);
    const [showSearch, setShowSearch] = useState(false);
    const [searchText, setSearchText] = useState('');
    const [categories, setCategories] = useState([]);
    const [subCategories, setSubCategories] = useState([]);
    const [showDetailModal, setShowDetailModal] = useState(false);
    const [selectedCategory, setSelectedCategory] = useState(null);

    const [newCategory, setNewCategory] = useState({
        categoryName: '',
        slug: '',
        status: 'Active',
        description: '',
        icon: ''
    });

    const baseUrl = 'https://localhost:7086/api/Category';

    useEffect(() => {
        fetchCategories();
        fetchSubCategories();
    }, []);

    const fetchCategories = async () => {
        try {
            const response = await axios.get(`${baseUrl}/GetCategories`);
            setCategories(response.data);
        } catch (error) {
            console.error('Error fetching categories:', error);
        }
    };

    const fetchSubCategories = async () => {
        try {
            const response = await axios.get(`${baseUrl}/GetSubCategories`);
            setSubCategories(response.data);
        } catch (error) {
            console.error('Error fetching subcategories:', error);
        }
    };

    const handleAddCategory = async () => {
        try {
            await axios.post(`${baseUrl}/AddCategory`, newCategory);
            fetchCategories();
            resetModal();
        } catch (error) {
            console.error('Error adding category:', error);
        }
    };

    const handleUpdateCategory = async () => {
        try {
            await axios.put(`${baseUrl}/EditCategory/${editCategoryId}`, {
                id: editCategoryId,
                ...newCategory
            });
            fetchCategories();
            resetModal();
        } catch (error) {
            console.error('Error updating category:', error);
        }
    };

    const handleDeleteCategory = async (id) => {
        try {
            await axios.delete(`${baseUrl}/GetPages/${id}`);
            fetchCategories();
        } catch (error) {
            console.error('Error deleting category:', error);
        }
    };

    const handleReadMore = (category) => {
        setSelectedCategory(category);
        setShowDetailModal(true);
    };

    const resetModal = () => {
        setNewCategory({
            categoryName: '',
            slug: '',
            status: 'Active',
            description: '',
            icon: ''
        });
        setIsEditMode(false);
        setEditCategoryId(null);
        setShowModal(false);
    };

    const handleEditClick = (category) => {
        setNewCategory({
            categoryName: category.categoryName || '',
            slug: category.slug || '',
            status: category.status || 'Active',
            description: category.description || '',
            icon: category.icon || ''
        });
        setEditCategoryId(category.id);
        setIsEditMode(true);
        setShowModal(true);
    };

    const iconOptions = [
        ...Object.keys(FaIcons).map(name => ({ value: name, label: name, icon: FaIcons[name] })),
        ...Object.keys(MdIcons).map(name => ({ value: name, label: name, icon: MdIcons[name] })),
        ...Object.keys(Io5Icons).map(name => ({ value: name, label: name, icon: Io5Icons[name] }))
    ];

    const selectedIconOption = iconOptions.find(opt => opt.value === newCategory.icon) || null;
    const customSingleValue = ({ data }) => (
        <div style={{ display: "flex", alignItems: "center" }}>
            {data.icon && <data.icon style={{ marginRight: 10 }} />}
            {data.label}
        </div>
    );

    const customOption = (props) => {
        const { data, innerRef, innerProps } = props;
        const Icon = data.icon;
        return (
            <div
                ref={innerRef}
                {...innerProps}
                style={{
                    display: "flex",
                    alignItems: "center",
                    padding: 10,
                    cursor: "pointer",
                }}
            >
                {Icon && <Icon style={{ marginRight: 10 }} />}
                {data.label}
            </div>
        );
    };

  const customStyles = {
    control: (provided, state) => ({
        ...provided,
        padding: '5px 2px',
        border: '1px solid #ff5e6c',       // var(--secondary-text)
        borderRadius: '6px',
        color: '#0582ca',                  // var(--primary-text)
        width: '100%',
        fontSize: '14px',
        outline: 'none',
        marginBottom: '12px',
        transition: 'border 0.3s ease, box-shadow 0.3s ease',
        // boxShadow: state.isFocused ? '0 0 4px rgba(5, 130, 202, 0.4)' : 'none' // light glow using var(--primary-text)
    }),
    option: (provided, state) => ({
        ...provided,
        padding: '8px 12px',
        backgroundColor: state.isFocused ? '#fff5d7' : 'white',  // var(--light-yellow)
        color: state.isFocused ? '#ff5e6c' : '#0582ca',          // hover: secondary, default: primary
        cursor: 'pointer',
    }),
    singleValue: (provided) => ({
        ...provided,
        color: '#0582ca',  // var(--primary-text)
    }),
    menu: (provided) => ({
        ...provided,
        zIndex: 10,
    }),
};

    return (
        <div className='category-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='category-container'>
                    <div className="category-header">
                        <h2>Category</h2>
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
                                    placeholder="Search category..."
                                    value={searchText}
                                    onChange={e => setSearchText(e.target.value)}
                                />
                            )}
                            <button className='add-category-btn' onClick={() => setShowModal(true)}>
                                <FaPlus /> Add Category
                            </button>
                        </div>
                    </div>

                    <div className="category-list">
                        {categories
                            .filter(c => (c.categoryName || '').toLowerCase().includes(searchText.toLowerCase()))
                            .map(category => {
                                const IconComponent = iconOptions.find(i => i.value === category.icon)?.icon;
                                const relatedSubcategories = subCategories.filter(sub => sub.categoryId === category.id);
                                const previewSubcategories = relatedSubcategories.slice(0, 3);
                                const hasMore = relatedSubcategories.length > 3;

                                return (
                                    <div key={category.id} className="category-card">
                                        <div className="card-header">
                                            <h3>{category.categoryName}</h3>
                                            <div className="card-actions">
                                                <button onClick={() => handleEditClick(category)} className="edit-btn">Edit</button>
                                                <button onClick={() => handleDeleteCategory(category.id)} className="delete-btn">Delete</button>
                                            </div>
                                        </div>

                                        <ul className="subcategory-list">
                                            {previewSubcategories.map((sub, idx) => (
                                                <li key={idx}>{sub.subCategoryName}</li>
                                            ))}
                                        </ul>

                                        <div className="card-footer">
                                            {hasMore && (
                                                <a className="read-more" onClick={(e) => { e.preventDefault(); handleReadMore(category); }}>Read More →</a>
                                            )}
                                            {IconComponent && <div className="category-icon"><IconComponent size={60} color="#744253" /></div>}
                                        </div>
                                    </div>
                                );
                            })}
                    </div>

                    {showDetailModal && selectedCategory && (
                        <div className="detail-modal-overlay">
                            <div className="detail-modal">
                                <button className="close-btn" onClick={() => setShowDetailModal(false)}>×</button>
                                <div className="modal-content-box">
                                    <div className="modal-icon">
                                        {iconOptions.find(i => i.value === selectedCategory.icon)?.icon &&
                                            React.createElement(iconOptions.find(i => i.value === selectedCategory.icon).icon, { size: 50 })}
                                    </div>
                                    <h2>{selectedCategory.categoryName}</h2>
                                    <p className="by-admin">by Admin</p>
                                    <div className="description-block">
                                        <strong>Description</strong>
                                        <p>{selectedCategory.description}</p>
                                    </div>
                                    <div className="subcat-block">
                                        <strong>Sub Categories</strong>
                                        <ul>
                                            {subCategories.filter(sc => sc.categoryId === selectedCategory.id).map(sc => (
                                                <li key={sc.id}>{sc.subCategoryName}</li>
                                            ))}
                                        </ul>
                                    </div>
                                    <div className="modal-footer">
                                        <button className="delete-btn" onClick={() => handleDeleteCategory(selectedCategory.id)}>Delete</button>
                                        <button className="edit-btn" onClick={() => { handleEditClick(selectedCategory); setShowDetailModal(false); }}>Edit</button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    )}

                    {showModal && (
                        <div className='modal-overlay-category'>
                            <div className='modal-content-category'>
                                <h3>{isEditMode ? 'Edit Category' : 'Add New Category'}</h3>
                                <label>Category Name</label>
                                <input type='text' value={newCategory.categoryName} onChange={(e) => setNewCategory({ ...newCategory, categoryName: e.target.value })} />
                                <label>Slug</label>
                                <input type='text' value={newCategory.slug} onChange={(e) => setNewCategory({ ...newCategory, slug: e.target.value })} />
                                <label>Status</label>
                                <select value={newCategory.status} onChange={(e) => setNewCategory({ ...newCategory, status: e.target.value })}>
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>
                                <label>Choose Icon</label>
                                <Select options={iconOptions} value={selectedIconOption} onChange={(selected) => setNewCategory(prev => ({ ...prev, icon: selected ? selected.value : '' }))} components={{ Option: customOption, SingleValue: customSingleValue }} isClearable styles={customStyles} />
                                <label>Description</label>
                                <textarea value={newCategory.description} onChange={(e) => setNewCategory({ ...newCategory, description: e.target.value })} rows={3}></textarea>
                                <div className='modal-actions'>
                                    <button onClick={isEditMode ? handleUpdateCategory : handleAddCategory}>{isEditMode ? 'Update' : 'Submit'}</button>
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

export default Category