import React, { useState, useEffect } from 'react';
import '../Style/application.css';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import ComposeModal from '../Email/ComposeModal';
import {
    MdInbox, MdOutlineStar, MdSend, MdDrafts, MdDelete, MdReport,
    MdOutlineMarkEmailRead, MdOutlineMarkunread, MdNewLabel, MdOutlineStarBorder
} from 'react-icons/md';
import { FaEdit, FaFolderPlus } from 'react-icons/fa';
import { IoArchive } from 'react-icons/io5';
import { FaChevronLeft, FaChevronRight } from "react-icons/fa";

const PAGE_SIZE = 30;
const API_BASE = 'https://localhost:7210/api/Email'; // Change if deployed

function Inbox() {
       const username = localStorage.getItem("username") || localStorage.getItem("email") || "Unknown User";
    const email = localStorage.getItem("email") || "unknown@gmail.com";

    const [isComposeOpen, setIsComposeOpen] = useState(false);
    const [selectedTab, setSelectedTab] = useState("inbox");
    const [selectedEmail, setSelectedEmail] = useState(null);
    const [selectedEmails, setSelectedEmails] = useState([]);
    const [emails, setEmails] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [currentPage, setCurrentPage] = useState(0);

    useEffect(() => {
        fetchEmails();
    }, [selectedTab]);

    const fetchEmails = () => {
        setLoading(true);
        setError(null);

        let endpoint;
        switch (selectedTab) {
            case 'inbox':
                endpoint = `${API_BASE}/inbox`;
                break;
            case 'sent':
                endpoint = `${API_BASE}/sent`;
                break;
            case 'allmail':
                endpoint = `${API_BASE}/all`;
                break;
            case 'starred':
                endpoint = `${API_BASE}/starred`;
                break;
            default:
                endpoint = `${API_BASE}/inbox`;
        }

        fetch(endpoint)
            .then(res => {
                if (!res.ok) throw new Error('Failed to fetch emails');
                return res.json();
            })
            .then(data => {
                setEmails(data);
                setLoading(false);
                setCurrentPage(0);
                setSelectedEmails([]);
                setSelectedEmail(null);
            })
            .catch(err => {
                setError(err.message);
                setLoading(false);
            });
    };

    const performAction = (endpointSuffix, method = "POST") => {
        Promise.all(selectedEmails.map(id =>
            fetch(`${API_BASE}/${endpointSuffix}/${id}`, { method })
        ))
        .then(() => fetchEmails())
        .catch(err => console.error(`Error performing ${endpointSuffix}`, err));
    };

    const handleMoveToFolder = (folderName) => {
        Promise.all(selectedEmails.map(id =>
            fetch(`${API_BASE}/move/${id}?folder=${encodeURIComponent(folderName)}`, {
                method: 'POST',
            })
        ))
        .then(() => fetchEmails())
        .catch(err => console.error("Error moving emails:", err));
    };

    const handleLabelEmail = (labelName) => {
        Promise.all(selectedEmails.map(id =>
            fetch(`${API_BASE}/label/${id}?label=${encodeURIComponent(labelName)}`, {
                method: 'POST',
            })
        ))
        .then(() => fetchEmails())
        .catch(err => console.error("Error labeling emails:", err));
    };

    const paginatedEmails = emails.slice(currentPage * PAGE_SIZE, (currentPage + 1) * PAGE_SIZE);
    const totalPages = Math.ceil(emails.length / PAGE_SIZE);

    return (
        <div className='application-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='application-container'>
                    <div className='email-option'>
                        <div className='user-info'>
                            <div>
                                <strong>{username}</strong>
                                <div className='user-email'>{email}</div>
                            </div>
                        </div>

                        <button className='compose-btn' onClick={() => setIsComposeOpen(true)}>
                            <FaEdit /> Compose
                        </button>

                        <ul className='email-menu'>
                            <li className={selectedTab === "inbox" ? "active" : ""} onClick={() => setSelectedTab("inbox")}>
                                <MdInbox /> Inbox <span className='count'>{emails.length}</span>
                            </li>
                            <li className={selectedTab === "starred" ? "active" : ""} onClick={() => setSelectedTab("starred")}>
                                <MdOutlineStar /> Starred <span className='count'>0</span>
                            </li>
                            <li className={selectedTab === "sent" ? "active" : ""} onClick={() => setSelectedTab("sent")}>
                                <MdSend /> Sent <span className='count'>{emails.length}</span>
                            </li>
                            <li className={selectedTab === "allmail" ? "active" : ""} onClick={() => setSelectedTab("allmail")}>
                                <MdOutlineMarkEmailRead /> All Mail <span className='count'>{emails.length}</span>
                            </li>
                        </ul>
                    </div>

                    <div className='email-inbox'>
                        {selectedEmail ? (
                            <div className="email-detail-card">
                                <button className="back-btn" onClick={() => setSelectedEmail(null)}>Back</button>
                                <h3>{selectedEmail.subject}</h3>
                                <p><strong>From:</strong> {selectedEmail.fromEmail}</p>
                                <p><strong>To:</strong> {selectedEmail.toEmail}</p>
                                <p><strong>Date:</strong> {new Date(selectedEmail.sentTime || selectedEmail.receviedTime).toLocaleString()}</p>
                                <div className="email-body">{selectedEmail.body}</div>
                            </div>
                        ) : (
                            <>
                                <h2 style={{ marginTop: "4px" }}>
                                    {selectedTab.charAt(0).toUpperCase() + selectedTab.slice(1)} Emails
                                </h2>

                                <div className="pagination">
                                    <button onClick={() => setCurrentPage(p => Math.max(0, p - 1))} disabled={currentPage === 0}>
                                        <FaChevronLeft />
                                    </button>
                                    <span> Page {currentPage + 1} of {totalPages} </span>
                                    <button onClick={() => setCurrentPage(p => Math.min(totalPages - 1, p + 1))} disabled={currentPage >= totalPages - 1}>
                                        <FaChevronRight />
                                    </button>
                                </div>

                                {selectedEmails.length > 0 && (
                                    <div className="email-toolbar">
                                        <span>{selectedEmails.length} selected</span>
                                        <div className="email-actions">
                                            <button className="toolbar-btn delete" title="Delete"><MdDelete /></button>
                                            <button className="toolbar-btn archive" title="Archive"><IoArchive /></button>
                                            <button className="toolbar-btn read" title="Mark as Read"><MdOutlineMarkEmailRead /></button>
                                            <button className="toolbar-btn unread" title="Mark as Unread"><MdOutlineMarkunread /></button>
                                            <button className="toolbar-btn star" title="Star"><MdOutlineStar /></button>
                                            <button className="toolbar-btn unstar" title="Unstar"><MdOutlineStarBorder /></button>
                                            <button className="toolbar-btn spam" title="Spam"><MdReport /></button>
                                            <button className="toolbar-btn move" title="Move to Folder"><FaFolderPlus /></button>
                                            <button className="toolbar-btn label" title="Add Label"><MdNewLabel /></button>
                                        </div>
                                    </div>
                                )}

                                {loading ? (
                                    <p>Loading emails...</p>
                                ) : error ? (
                                    <p style={{ color: 'red' }}>{error}</p>
                                ) : (
                                    <div className="email-list">
                                        {paginatedEmails.map(email => (
                                            <div
                                                key={email.id}
                                                className={`email-card ${selectedEmails.includes(email.id) ? 'selected' : ''}`}
                                                onClick={() => setSelectedEmail(email)}
                                            >
                                                <div className="email-left" onClick={(e) => e.stopPropagation()}>
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedEmails.includes(email.id)}
                                                        onChange={(e) => {
                                                            if (e.target.checked) {
                                                                setSelectedEmails(prev => [...prev, email.id]);
                                                            } else {
                                                                setSelectedEmails(prev => prev.filter(id => id !== email.id));
                                                            }
                                                        }}
                                                    />
                                                    <div className="avatar">
                                                        {(selectedTab === 'inbox' ? email.fromEmail : email.toEmail)?.charAt(0)}
                                                    </div>
                                                    <div className="email-info">
                                                        <div className="sender">
                                                            {selectedTab === 'inbox' ? email.fromEmail : email.toEmail}
                                                        </div>
                                                        <div className="subject">{email.subject}</div>
                                                        <div className="preview">{email.body?.substring(0, 60)}...</div>
                                                    </div>
                                                </div>
                                                <div className="email-right">
                                                    <span className="timestamp">
                                                        {new Date(email.sentTime || email.receviedTime).toLocaleString()}
                                                    </span>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </>
                        )}
                    </div>
                </div>
            </div>

            <ComposeModal isOpen={isComposeOpen} onClose={() => setIsComposeOpen(false)} />
        </div>
    );
}

export default Inbox;
