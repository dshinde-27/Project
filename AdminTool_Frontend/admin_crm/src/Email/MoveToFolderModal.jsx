import React, { useState, useEffect } from 'react';
import '../Style/application.css';

const API_BASE = 'https://localhost:7210/api/Email';

function MoveToFolderModal({ isOpen, onClose, onSubmit }) {
    const [folderName, setFolderName] = useState('');
    const [selectedRule, setSelectedRule] = useState('');
    const [emailDomains, setEmailDomains] = useState([]);
    const [error, setError] = useState(null);

    useEffect(() => {
        if (isOpen) {
            fetch(`${API_BASE}/email-domains`)
                .then(res => {
                    if (!res.ok) throw new Error("Network response was not ok");
                    return res.json();
                })
                .then(data => {
                    setEmailDomains(Array.isArray(data) ? data : []);
                    setError(null);
                })
                .catch(err => {
                    console.error("Failed to load domain rules:", err);
                    setError("Failed to load rules.");
                });
        }
    }, [isOpen]);

    const handleSubmit = () => {
        if (!folderName.trim()) {
            alert("Please enter a folder name.");
            return;
        }

        onSubmit({ folderName: folderName.trim(), rule: selectedRule || null });

        // Clear modal state after submission
        setFolderName('');
        setSelectedRule('');
        onClose();
    };

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h3>Move to Folder</h3>

                <label htmlFor="folderName">
                    Folder Name:
                    <input
                        id="folderName"
                        type="text"
                        value={folderName}
                        onChange={(e) => setFolderName(e.target.value)}
                        placeholder="Folder name"
                        required
                    />
                </label>

                <label htmlFor="ruleSelect">
                    Choose Rule (optional):
                    <select
                        id="ruleSelect"
                        value={selectedRule}
                        onChange={(e) => setSelectedRule(e.target.value)}
                    >
                        <option value="">-- No Rule --</option>
                        {emailDomains.length > 0 ? (
                            emailDomains.map(domain => (
                                <option key={domain} value={domain}>{domain}</option>
                            ))
                        ) : (
                            <option disabled>No rules available</option>
                        )}
                    </select>
                </label>

                {error && <p className="error">{error}</p>}

                <div className="modal-actions">
                    <button className="btn btn-primary" onClick={handleSubmit}>Move</button>
                    <button className="btn cancel" onClick={onClose}>Cancel</button>
                </div>
            </div>
        </div>
    );
}

export default MoveToFolderModal;
