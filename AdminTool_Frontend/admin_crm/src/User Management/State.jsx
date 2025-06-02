import React, { useState, useEffect } from 'react';
import Sidebar from '../Layout/Sidebar'
import '../Style/userManagement.css';
import { FaPlus, FaSearch, FaEdit, FaTrash, FaUserShield } from "react-icons/fa";
import Navbar from '../Layout/Navbar';
import axios from 'axios';

function State() {
    const [showSearch, setShowSearch] = useState(false);
    const [searchText, setSearchText] = useState('');

    const [states, setStates] = useState([]);
    const [countries, setCountries] = useState([]);
    const [showModal, setShowModal] = useState(false);
    const [newState, setNewState] = useState({
        stateName: '',
        countryId: '',
        status: 'Active',
    });


    const AddStateUrl = 'https://localhost:7210/api/Location/AddState';
    const GetStateUrl = 'https://localhost:7210/api/Location/GetStates';
    const DeleteStateUrl = 'https://localhost:7210/api/Location/DeleteState';
    const GetCountryUrl = 'https://localhost:7210/api/Location/GetCountries';

    useEffect(() => {
        fetchStates();
        fetchCountries();
    }, []);

    const fetchStates = async () => {
        try {
            const response = await axios.get(GetStateUrl);
            setStates(response.data);
        } catch (error) {
            console.error('Error fetching states:', error);
        }
    };

    const fetchCountries = async () => {
        try {
            const response = await axios.get(GetCountryUrl);
            setCountries(response.data);
        } catch (error) {
            console.error('Error fetching countries:', error);
        }
    };

    const handleAddState = async () => {
        try {
            await axios.post(AddStateUrl, newState);
            setNewState({ stateName: '', countryId: '', status: 'Active' });
            fetchStates();
            setShowModal(false);
        } catch (error) {
            if (error.response) {
                console.error('Server response:', error.response.data);
            } else {
                console.error('Error adding state:', error);
            }
        }
    };


    const handleDeleteState = async (id) => {
        try {
            await axios.delete(`${DeleteStateUrl}/${id}`);
            // toast.success('State deleted successfully!');
            fetchStates();
        } catch (error) {
            //toast.error('Failed to delete state');
            console.error('Error deleting state:', error);
        }
    };
    return (
        <div className='userManage-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='userManage-container'>
                    <div className="userManage-header">
                        <h2>State</h2>
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
                                    placeholder="Search state..."
                                    value={searchText}
                                    onChange={e => setSearchText(e.target.value)}
                                />
                            )}
                            <button className='add-userManage-btn' onClick={() => setShowModal(true)}>
                                <FaPlus /> Add State
                            </button>
                        </div>
                    </div>

                    <div className='userManage-list'>
                        <table className='userManage-table'>
                            <thead>
                                <tr>
                                    <th>State</th>
                                    <th>Country</th>
                                    <th>Status</th>
                                    <th>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                {states.map((state) => (
                                    <tr key={state.id}>
                                        <td>{state.stateName}</td>
                                        <td>{state.countryName}</td>
                                        <td>
                                            <span className={`status ${state.status.toLowerCase()}`}>
                                                {state.status}
                                            </span>
                                        </td>
                                        <td className='action-buttons'>
                                            <FaEdit className='icon edit' title='Edit' />
                                            <FaTrash className='icon delete' title='Delete' onClick={() => handleDeleteState(state.id)} />
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {showModal && (
                        <div className='modal-overlay'>
                            <div className='modal-content'>
                                <h3>Add New State</h3>

                                <label>State Name</label>
                                <input
                                    type='text'
                                    value={newState.stateName}
                                    onChange={(e) => setNewState({ ...newState, stateName: e.target.value })}
                                />

                                <label>Country</label>
                                <select
                                    value={newState.countryId}
                                    onChange={(e) => setNewState({ ...newState, countryId: e.target.value })}
                                >
                                    <option value="">-- Select Country --</option>
                                    {countries.map(country => (
                                        <option key={country.id} value={country.id}>
                                            {country.countryName}
                                        </option>
                                    ))}
                                </select>

                                <label>Status</label>
                                <select
                                    value={newState.status}
                                    onChange={(e) => setNewState({ ...newState, status: e.target.value })}
                                >
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>

                                <div className='modal-actions'>
                                    <button
                                        onClick={handleAddState}
                                        disabled={!newState.stateName || !newState.countryId}
                                    >
                                        Submit
                                    </button>
                                    <button className='cancel-btn' onClick={() => setShowModal(false)}>Cancel</button>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}

export default State