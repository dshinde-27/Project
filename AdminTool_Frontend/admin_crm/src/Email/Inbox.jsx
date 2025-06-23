import React, { useState, useEffect } from 'react';
import '../Style/application.css';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import ComposeModal from '../Email/ComposeModal';
import MoveToFolderModal from '../Email/MoveToFolderModal';
import { FaFolder } from "react-icons/fa6";

import {
    MdInbox, MdOutlineStar, MdSend, MdDrafts, MdDelete, MdReport,
    MdOutlineMarkEmailRead, MdOutlineMarkunread, MdNewLabel,
    MdOutlineStarBorder, MdOutlineStar as MdStarFilled
} from 'react-icons/md';

import { FaEdit, FaFolderPlus, FaChevronLeft, FaChevronRight } from "react-icons/fa";
import { IoArchive } from 'react-icons/io5';

const PAGE_SIZE = 10;
const API_BASE = 'https://localhost:7210/api/Email';

function Inbox() {
    const username = localStorage.getItem("username") || localStorage.getItem("email") || "Unknown User";
    const email = localStorage.getItem("email") || "unknown@gmail.com";

    const [isComposeOpen, setIsComposeOpen] = useState(false);
    const [selectedTab, setSelectedTab] = useState("inbox");
    const [selectedEmail, setSelectedEmail] = useState(null);
    const [selectedEmails, setSelectedEmails] = useState([]);
    const [emails, setEmails] = useState([]);
    const [paginatedEmails, setPaginatedEmails] = useState([]);
    const [currentPage, setCurrentPage] = useState(0);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    const [inboxCount, setInboxCount] = useState(0);
    const [sentCount, setSentCount] = useState(0);
    const [starredCount, setStarredCount] = useState(0);
    const [archiveCount, setArchiveCount] = useState(0);
    const [trashCount, setTrashCount] = useState(0);

    const totalPages = Math.ceil(emails.length / PAGE_SIZE);
    const [isMoveModalOpen, setIsMoveModalOpen] = useState(false);

    useEffect(() => {
        setCurrentPage(0);
        setSelectedEmails([]); // Clear selected emails on tab switch
        fetchEmails();
    }, [selectedTab]);

    useEffect(() => {
        fetch(`${API_BASE}/inbox`).then(res => res.json()).then(data => setInboxCount(data.length));
        fetch(`${API_BASE}/sent`).then(res => res.json()).then(data => setSentCount(data.length));
        fetch(`${API_BASE}/starred`).then(res => res.json()).then(data => setStarredCount(data.length));
        fetch(`${API_BASE}/archive`).then(res => res.json()).then(data => setArchiveCount(data.length));
        fetch(`${API_BASE}/deleted`).then(res => res.json()).then(data => setTrashCount(data.length));
    }, []);

    useEffect(() => {
        setPaginatedEmails(
            emails.slice(currentPage * PAGE_SIZE, (currentPage + 1) * PAGE_SIZE)
        );
    }, [emails, currentPage]);

    const fetchEmails = () => {
        setLoading(true);
        setError(null);

        let endpoint;

        switch (selectedTab.toLowerCase()) {
            case "inbox":
                endpoint = "/inbox";
                break;
            case "sent":
                endpoint = "/sent";
                break;
            case "starred":
                endpoint = "/starred";
                break;
            case "allmail":
                endpoint = "/emails";
                break;
            case "archive":
                endpoint = "/archived";
                break;
            case "trash":
                endpoint = "/deleted";
                break;
            default:
                // Assume it's a custom folder
                endpoint = `/folder/${encodeURIComponent(selectedTab)}`;
                break;
        }

        fetch(`${API_BASE}${endpoint}`)
            .then(res => {
                if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
                return res.json();
            })
            .then(data => {
                setEmails(Array.isArray(data) ? data : []);
                setLoading(false);
            })
            .catch(err => {
                console.error("Error fetching emails:", err);
                setError("Failed to load emails.");
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

    const handleStarEmails = () => performAction("star");
    const handleUnstarEmails = () => performAction("unstar");

    const handleLabelEmails = () => {
        const label = prompt("Enter label name:");
        if (label) {
            Promise.all(selectedEmails.map(id =>
                fetch(`${API_BASE}/label/${id}?label=${encodeURIComponent(label)}`, { method: 'POST' })
            ))
                .then(() => fetchEmails())
                .catch(err => console.error("Error labeling emails:", err));
        }
    };

    const handleDeleteEmails = async () => {
        try {
            for (const id of selectedEmails) {
                await fetch(`${API_BASE}/delete/${id}`, { method: 'DELETE' });
            }
            fetchEmails();
            setSelectedEmails([]);
        } catch (error) {
            console.error("Error deleting emails:", error);
        }
    };

    const handleArchiveEmails = async () => {
        try {
            for (const id of selectedEmails) {
                await fetch(`${API_BASE}/archive/${id}`, { method: 'POST' });
            }
            fetchEmails();
        } catch (error) {
            console.error("Error archiving emails:", error);
        }
    };

    const handleSpamEmails = () => {
        alert("Spam functionality not implemented yet.");
    };

    const handleMoveToFolder = ({ folderName, rule }) => {
        fetch(`${API_BASE}/move?folder=${encodeURIComponent(folderName)}&rule=${encodeURIComponent(rule)}`, {
            method: 'POST'
        })
            .then(res => res.ok ? res.text() : Promise.reject("Failed to move"))
            .then(msg => {
                alert(msg);

                // Avoid adding duplicates (case-insensitive match)
                setCustomFolders(prev => {
                    const exists = prev.some(f => f.toLowerCase() === folderName.toLowerCase());
                    return exists ? prev : [...prev, folderName];
                });

                fetchEmails(); // Refresh list
            })
            .catch(err => {
                console.error(err);
                alert("Error moving emails.");
            });
    };

    const [customFolders, setCustomFolders] = useState(() => {
        const saved = localStorage.getItem("customFolders");
        return saved ? JSON.parse(saved) : [];
    });

    useEffect(() => {
        localStorage.setItem("customFolders", JSON.stringify(customFolders));
    }, [customFolders]);

    useEffect(() => {
        fetch(`${API_BASE}/folders`)
            .then(res => res.json())
            .then(data => {
                if (Array.isArray(data)) {
                    setCustomFolders(data);
                }
            })
            .catch(err => console.error("Failed to load folders", err));
    }, []);

    return (
        <div className='application-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='application-container'>
                    <div className='email-option'>
                        <div className='user-info'>
                            <strong>{username}</strong>
                            <div className='user-email'>{email}</div>
                        </div>

                        <button className='compose-btn' onClick={() => setIsComposeOpen(true)}>
                            <FaEdit /> Compose
                        </button>

                        <ul className='email-menu'>
                            <li className={selectedTab === "inbox" ? "active" : ""} onClick={() => setSelectedTab("inbox")}>
                                <MdInbox /> Inbox <span className='count'>{inboxCount}</span>
                            </li>
                            <li className={selectedTab === "starred" ? "active" : ""} onClick={() => setSelectedTab("starred")}>
                                <MdOutlineStar /> Starred <span className='count'>{starredCount}</span>
                            </li>
                            <li className={selectedTab === "sent" ? "active" : ""} onClick={() => setSelectedTab("sent")}>
                                <MdSend /> Sent <span className='count'>{sentCount}</span>
                            </li>
                            <li className={selectedTab === "allmail" ? "active" : ""} onClick={() => setSelectedTab("allmail")}>
                                <MdOutlineMarkEmailRead /> All Mail <span className='count'>{inboxCount}</span>
                            </li>
                            <li className={selectedTab === "archive" ? "active" : ""} onClick={() => setSelectedTab("archive")}>
                                <IoArchive /> Archive <span className='count'>{archiveCount}</span>
                            </li>
                            <li className={selectedTab === "trash" ? "active" : ""} onClick={() => setSelectedTab("trash")}>
                                <MdDelete /> Trash <span className='count'>{trashCount}</span>
                            </li>
                            {customFolders.map(folder => (
                                <li key={folder} className={selectedTab === folder ? "active" : ""} onClick={() => setSelectedTab(folder)} style={{marginRight:"98px"}}>
                                    <FaFolder/> {folder}
                                </li>
                            ))}

                        </ul>
                    </div>

                    <div className='email-inbox'>
                        {!selectedEmail ? (
                            <>
                                <h2>{selectedTab.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase())} Emails</h2>

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
                                            <button className="toolbar-btn delete" onClick={handleDeleteEmails} disabled={!selectedEmails.length}><MdDelete /></button>
                                            <button className="toolbar-btn archive" onClick={handleArchiveEmails} disabled={!selectedEmails.length}><IoArchive /></button>
                                            <button className="toolbar-btn star" onClick={handleStarEmails} disabled={!selectedEmails.length}><MdStarFilled /></button>
                                            <button className="toolbar-btn unstar" onClick={handleUnstarEmails} disabled={!selectedEmails.length}><MdOutlineStarBorder /></button>
                                            <button className="toolbar-btn move" onClick={() => setIsMoveModalOpen(true)} disabled={!selectedEmails.length}><FaFolderPlus /></button>
                                            <button className="toolbar-btn label" onClick={handleLabelEmails} disabled={!selectedEmails.length}><MdNewLabel /></button>
                                            <button className="toolbar-btn spam" onClick={handleSpamEmails}><MdReport /></button>
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
                                                className={`email-card ${selectedEmails.includes(email.id) ? 'selected' : ''} ${!email.isRead ? 'unread' : ''}`}
                                                onClick={() => {
                                                    setSelectedEmail(email);
                                                    if (!email.isRead) {
                                                        fetch(`${API_BASE}/markasread/${email.id}`, { method: "POST" })
                                                            .then(() => {
                                                                setEmails(prev =>
                                                                    prev.map(e => e.id === email.id ? { ...e, isRead: true } : e)
                                                                );
                                                            });
                                                    }
                                                }}
                                            >
                                                <div className="email-left" onClick={(e) => e.stopPropagation()}>
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedEmails.includes(email.id)}
                                                        onChange={(e) => {
                                                            setSelectedEmails(prev =>
                                                                e.target.checked
                                                                    ? [...prev, email.id]
                                                                    : prev.filter(id => id !== email.id)
                                                            );
                                                        }}
                                                    />
                                                    <div className="avatar">
                                                        {(selectedTab === 'inbox' ? email.fromEmail : email.toEmail)?.charAt(0)}
                                                    </div>
                                                    <div className="email-info">
                                                        <div className="sender">{selectedTab === 'inbox' ? email.fromEmail : email.toEmail}</div>
                                                        <div className="subject">{email.subject}</div>
                                                        <div className="preview">{email.body?.substring(0, 60)}...</div>
                                                    </div>
                                                </div>
                                                <div className="email-right">
                                                    <span className="timestamp">
                                                        {new Date(email.sentTime || email.receivedTime).toLocaleString('en-US', {
                                                            month: 'short',
                                                            day: 'numeric',
                                                            hour: '2-digit',
                                                            minute: '2-digit',
                                                        })}
                                                    </span>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </>
                        ) : (
                            <div className="email-detail-card">
                                <button className="back-btn" onClick={() => setSelectedEmail(null)}>Back</button>
                                <h3>{selectedEmail.subject}</h3>
                                <p><strong>From:</strong> {selectedEmail.fromEmail}</p>
                                <p><strong>To:</strong> {selectedEmail.toEmail}</p>
                                <p><strong>Date:</strong> {new Date(selectedEmail.sentTime || selectedEmail.receivedTime).toLocaleString()}</p>
                                <div className="email-body">{selectedEmail.body}</div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <MoveToFolderModal
                isOpen={isMoveModalOpen}
                onClose={() => setIsMoveModalOpen(false)}
                onSubmit={handleMoveToFolder}
            />

            <ComposeModal isOpen={isComposeOpen} onClose={() => setIsComposeOpen(false)} />
        </div>
    );
}

export default Inbox;
