import React, { useState, useEffect } from 'react';
import axios from 'axios';
import Sidebar from '../Layout/Sidebar';
import Navbar from '../Layout/Navbar';
import { FaPlus, FaSearch, FaEdit, FaTrash } from 'react-icons/fa';
import '../Style/product.css';

function Attribute() {
  const [showModal, setShowModal] = useState(false);
  const [showSearch, setShowSearch] = useState(false);
  const [searchText, setSearchText] = useState('');

  const [attributeName, setAttributeName] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [subCategoryId, setSubCategoryId] = useState('');
  const [values, setValues] = useState([]);

  const [categories, setCategories] = useState([]);
  const [subCategories, setSubCategories] = useState([]);
  const [attributes, setAttributes] = useState([]);
  const [editingId, setEditingId] = useState(null);

  const CategoryUrl = 'https://localhost:7086/api/Category';
  const AttributeApiUrl = 'https://localhost:7086/api/Attribute';

  useEffect(() => {
    fetchCategories();
    fetchSubCategories();
    fetchAttributes();
  }, []);

  const fetchCategories = async () => {
    try {
      const response = await axios.get(`${CategoryUrl}/GetCategories`);
      setCategories(response.data);
    } catch (error) {
      console.error('Error fetching Categories:', error);
    }
  };

  const fetchSubCategories = async () => {
    try {
      const response = await axios.get(`${CategoryUrl}/GetSubCategories`);
      setSubCategories(response.data);
    } catch (error) {
      console.error('Error fetching SubCategories:', error);
    }
  };

  const fetchAttributes = async () => {
    try {
      const response = await axios.get(`${AttributeApiUrl}/GetAttributes`);
      setAttributes(response.data);
    } catch (error) {
      console.error('Error fetching Attributes:', error);
    }
  };

  const handleAddValue = () => {
    setValues([...values, { value: '', meta: '' }]);
  };

  const handleValueChange = (index, field, value) => {
    const updated = [...values];
    updated[index][field] = value;
    setValues(updated);
  };

  const handleRemoveValue = (index) => {
    const updated = [...values];
    updated.splice(index, 1);
    setValues(updated);
  };

  const resetForm = () => {
    setAttributeName('');
    setCategoryId('');
    setSubCategoryId('');
    setValues([]);
    setEditingId(null);
  };

  const handleSaveAttribute = async () => {
    if (!attributeName || !categoryId || !subCategoryId) {
      alert('Please fill all required fields.');
      return;
    }

    const attributeData = {
      attributeName,
      categoryId: Number(categoryId),
      subCategoryId: Number(subCategoryId),
      status: 'Active',
      values: values.map((v) => ({ value: v.value, meta: v.meta })),
    };

    try {
      if (editingId) {
        const response = await axios.put(
          `${AttributeApiUrl}/EditAttribute/${editingId}`,
          attributeData
        );
        if (response.data.success) {
          alert(response.data.message);
          setShowModal(false);
          fetchAttributes();
          resetForm();
        }
      } else {
        const response = await axios.post(
          `${AttributeApiUrl}/AddAttribute`,
          attributeData
        );
        if (response.data.success) {
          alert(response.data.message);
          setShowModal(false);
          fetchAttributes();
          resetForm();
        }
      }
    } catch (error) {
      console.error('Error saving attribute:', error);
      alert('Failed to save attribute.');
    }
  };

  const handleDeleteTag = async (id) => {
    if (!window.confirm('Are you sure you want to delete this attribute?')) return;

    try {
      const response = await axios.delete(`${AttributeApiUrl}/DeleteAttribute/${id}`);
      if (response.data.success) {
        alert(response.data.message);
        fetchAttributes();
      }
    } catch (error) {
      console.error('Error deleting attribute:', error);
      alert('Failed to delete attribute.');
    }
  };

  const handleEditClick = (attr) => {
    setShowModal(true);
    setEditingId(attr.id);
    setAttributeName(attr.attributeName);
    setCategoryId(attr.categoryId);
    setSubCategoryId(attr.subCategoryId);
    setValues(attr.values || []);
  };

  return (
    <div className="category-page">
      <Navbar />
      <div className="main-layout">
        <Sidebar />
        <div className="category-container">
          <div className="category-header">
            <h2>Attributes</h2>
            <div className="category-actions">
              <button
                className="search-icon"
                onClick={() => setShowSearch((prev) => !prev)}
              >
                <FaSearch />
              </button>
              {showSearch && (
                <input
                  type="text"
                  className="search-box"
                  placeholder="Search attributes..."
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                />
              )}
              <button className="add-category-btn" onClick={() => { setShowModal(true); resetForm(); }}>
                <FaPlus /> Add Attribute
              </button>
            </div>
          </div>

          <div className="category-list">
              <table className="category-table">
                <thead>
                  <tr>
                    <th>Id</th>
                    <th>Attribute Name</th>
                    <th>Values</th>
                    <th>Status</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {attributes
                    .filter((attr) =>
                      attr.attributeName
                        .toLowerCase()
                        .includes(searchText.toLowerCase())
                    )
                    .map((attr) => (
                      <tr key={attr.id}>
                        <td>{attr.id}</td>
                        <td>{attr.attributeName}</td>
                        <td>{(attr.values || []).map((v) => v.value).join(', ')}</td>
                        <td>
                          <span className={`status ${attr.status.toLowerCase()}`}>
                            {attr.status}
                          </span>
                        </td>
                        <td className="action-buttons">
                          <FaEdit
                            className="icon edit"
                            title="Edit"
                            onClick={() => handleEditClick(attr)}
                          />
                          <FaTrash
                            className="icon delete"
                            title="Delete"
                            onClick={() => handleDeleteTag(attr.id)}
                          />
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
          </div>

          {/* Modal */}
          {showModal && (
            <div className="modal-overlay-category">
              <div className="modal-content-attribute">
                <h3>{editingId ? 'Edit Attribute' : 'Add New Attribute'}</h3>
                <label>Attribute Name</label>
                <input
                  type="text"
                  placeholder="Attribute Name"
                  value={attributeName}
                  onChange={(e) => setAttributeName(e.target.value)}
                />

                <label>Category</label>
                <select
                  value={categoryId}
                  onChange={(e) => {
                    setCategoryId(e.target.value);
                    setSubCategoryId('');
                  }}
                >
                  <option value="">Select Category</option>
                  {categories.map((cat) => (
                    <option key={cat.id} value={cat.id}>
                      {cat.categoryName}
                    </option>
                  ))}
                </select>

                <label>Sub Category</label>
                <select
                  value={subCategoryId}
                  onChange={(e) => setSubCategoryId(e.target.value)}
                >
                  <option value="">Select Sub Category</option>
                  {subCategories
                    .filter(
                      (sub) => Number(sub.categoryId) === Number(categoryId)
                    )
                    .map((sub) => (
                      <option key={sub.id} value={sub.id}>
                        {sub.subCategoryName}
                      </option>
                    ))}
                </select>

                {/* Values */}
                {values.length === 0 ? (
                  <button className="btn-add-value" onClick={handleAddValue}>
                    + Add Value
                  </button>
                ) : (
                  <>
                    {values.map((item, index) => (
                      <div key={index} className="value-row">
                        <input
                          type="text"
                          placeholder="Value"
                          value={item.value}
                          onChange={(e) =>
                            handleValueChange(index, 'value', e.target.value)
                          }
                        />
                        <input
                          type="text"
                          placeholder="Meta"
                          value={item.meta}
                          onChange={(e) =>
                            handleValueChange(index, 'meta', e.target.value)
                          }
                        />
                        <button
                          className="btn-remove"
                          onClick={() => handleRemoveValue(index)}
                        >
                          Remove
                        </button>
                      </div>
                    ))}
                    <button className="btn-add-value" onClick={handleAddValue}>
                      Add More Values
                    </button>
                  </>
                )}

                <div className="modal-actions">
                  <button onClick={handleSaveAttribute}>Save</button>
                  <button
                    onClick={() => {
                      setShowModal(false);
                      resetForm();
                    }}
                  >
                    Cancel
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default Attribute;
