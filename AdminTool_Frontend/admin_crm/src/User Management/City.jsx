import React, { useState, useEffect } from 'react';
import Sidebar from '../Layout/Sidebar'
import '../Style/userManagement.css';
import { FaPlus, FaSearch, FaEdit, FaTrash, FaUserShield } from "react-icons/fa";
import Navbar from '../Layout/Navbar';
import axios from 'axios';

function City() {
    const [showSearch, setShowSearch] = useState(false);
    const [searchText, setSearchText] = useState('');
    const [cities, setCities] = useState([]);
    const [countries, setCountries] = useState([]);
    const [states, setStates] = useState([]);
    const [showModal, setShowModal] = useState(false);
    const [newCity, setNewCity] = useState({
        cityName: '',
        stateId: '',
        countryId: '',
        status: 'Active',
    });

    const AddCityUrl = 'https://localhost:7210/api/Location/AddCity';
    const GetCityUrl = 'https://localhost:7210/api/Location/GetCities';
    const GetStateUrl = 'https://localhost:7210/api/Location/GetStates';
    const GetCountryUrl = 'https://localhost:7210/api/Location/GetCountries';

    useEffect(() => {
        fetchCities();
        fetchCountries();
        fetchStates();
    }, []);

    const fetchCities = async () => {
        try {
            const response = await axios.get(GetCityUrl);
            setCities(response.data);
        } catch (error) {
            console.error('Error fetching cities:', error);
        }
    };

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

    const handleAddCity = async () => {
        try {
            await axios.post(AddCityUrl, newCity);
            fetchCities();
            setNewCity({ cityName: '', stateId: '', countryId: '', status: 'Active' });
            setShowModal(false);
        } catch (error) {
            console.error('Error adding city:', error);
        }
    };

    const handleDeleteCity = async (id) => {
        console.log('Delete city with ID:', id);
        // Add delete API call if needed
    };
    return (
        <div className='userManage-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='userManage-container'>
                    <div className="userManage-header">
                        <h2>City</h2>
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
                                    placeholder="Search city..."
                                    value={searchText}
                                    onChange={e => setSearchText(e.target.value)}
                                />
                            )}
                            <button className='add-userManage-btn' onClick={() => setShowModal(true)}>
                                <FaPlus /> Add City
                            </button>
                        </div>
                    </div>

                    <div className='userManage-list'>
                        <table className='userManage-table'>
                            <thead>
                                <tr>
                                    <th>City</th>
                                    <th>State</th>
                                    <th>Country</th>
                                    <th>Status</th>
                                    <th>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                {cities.map((city) => (
                                    <tr key={city.id}>
                                        <td>{city.cityName}</td>
                                        <td>{city.stateName}</td>
                                        <td>{city.countryName}</td>
                                        <td>
                                            <span className={`status ${city.status.toLowerCase()}`}>
                                                {city.status}
                                            </span>
                                        </td>
                                        <td className='action-buttons'>
                                            <FaEdit className='icon edit' title='Edit' />
                                            <FaTrash className='icon delete' title='Delete' onClick={() => handleDeleteCity(city.id)} />
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {showModal && (
                        <div className='modal-overlay'>
                            <div className='modal-content'>
                                <h3>Add New City</h3>

                                <label>City Name</label>
                                <input
                                    type='text'
                                    value={newCity.cityName}
                                    onChange={(e) => setNewCity({ ...newCity, cityName: e.target.value })}
                                />

                                <label>State</label>
                                <select
                                    value={newCity.stateId}
                                    onChange={(e) => setNewCity({ ...newCity, stateId: e.target.value })}
                                >
                                    <option value="">Select State</option>
                                    {states.map(state => (
                                        <option key={state.id} value={state.id}>
                                            {state.stateName}
                                        </option>
                                    ))}
                                </select>

                                <label>Country</label>
                                <select
                                    value={newCity.countryId}
                                    onChange={(e) => setNewCity({ ...newCity, countryId: e.target.value })}
                                >
                                    <option value="">Select Country</option>
                                    {countries.map(country => (
                                        <option key={country.id} value={country.id}>
                                            {country.countryName}
                                        </option>
                                    ))}
                                </select>


                                <label>Status</label>
                                <select
                                    value={newCity.status}
                                    onChange={(e) => setNewCity({ ...newCity, status: e.target.value })}
                                >
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>

                                <div className='modal-actions'>
                                    <button
                                        onClick={handleAddCity}
                                        disabled={!newCity.cityName || !newCity.stateId || !newCity.countryId}
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

export default City