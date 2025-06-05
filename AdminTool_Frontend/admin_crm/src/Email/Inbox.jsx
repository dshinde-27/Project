import React, { useState, useEffect } from 'react';
import '../Style/application.css';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import { MdInbox, MdOutlineStar, MdSend, MdDrafts, MdDelete, MdReport } from 'react-icons/md';
import { FaEdit } from "react-icons/fa";
import ComposeModal from '../Email/ComposeModal';

function Inbox() {
    const username = localStorage.getItem("username") || localStorage.getItem("email") || "Unknown User";
    const email = localStorage.getItem("email") || "unknown@gmail.com";
    const [isComposeOpen, setIsComposeOpen] = useState(false);

    // Sent emails state
    const [sentEmails, setSentEmails] = useState([]);
    const [loadingSent, setLoadingSent] = useState(false);
    const [errorSent, setErrorSent] = useState(null);

    // Inbox emails state
    const [inboxEmails, setInboxEmails] = useState([]);
    const [loadingInbox, setLoadingInbox] = useState(false);
    const [errorInbox, setErrorInbox] = useState(null);

    // Selected tab state
    const [selectedTab, setSelectedTab] = useState("inbox");

    // Fetch sent emails when tab is "sent"
    useEffect(() => {
        if (selectedTab === "sent") {
            setLoadingSent(true);
            fetch('https://localhost:7210/api/Email/sent')
                .then(res => {
                    if (!res.ok) throw new Error('Failed to fetch sent emails');
                    return res.json();
                })
                .then(data => {
                    setSentEmails(data);
                    setLoadingSent(false);
                    setErrorSent(null);
                })
                .catch(err => {
                    setErrorSent(err.message);
                    setLoadingSent(false);
                });
        }
    }, [selectedTab]);

    // Fetch inbox emails when tab is "inbox"
    useEffect(() => {
        if (selectedTab === "inbox") {
            setLoadingInbox(true);
            fetch('https://localhost:7210/api/Email/inbox')
                .then(res => {
                    if (!res.ok) throw new Error('Failed to fetch inbox emails');
                    return res.json();
                })
                .then(data => {
                    setInboxEmails(data);
                    setLoadingInbox(false);
                    setErrorInbox(null);
                })
                .catch(err => {
                    setErrorInbox(err.message);
                    setLoadingInbox(false);
                });
        }
    }, [selectedTab]);

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
                            <li
                                className={selectedTab === "inbox" ? "active" : ""}
                                onClick={() => setSelectedTab("inbox")}
                            >
                                <MdInbox /> Inbox <span className='count'>{inboxEmails.length}</span>
                            </li>
                            <li
                                className={selectedTab === "starred" ? "active" : ""}
                                onClick={() => setSelectedTab("starred")}
                            >
                                <MdOutlineStar /> Starred <span className='count'>46</span>
                            </li>
                            <li
                                className={selectedTab === "sent" ? "active" : ""}
                                onClick={() => setSelectedTab("sent")}
                            >
                                <MdSend /> Sent <span className='count'>{sentEmails.length}</span>
                            </li>
                            <li
                                className={selectedTab === "drafts" ? "active" : ""}
                                onClick={() => setSelectedTab("drafts")}
                            >
                                <MdDrafts /> Drafts <span className='count'>12</span>
                            </li>
                            <li
                                className={selectedTab === "deleted" ? "active" : ""}
                                onClick={() => setSelectedTab("deleted")}
                            >
                                <MdDelete /> Deleted <span className='count'>8</span>
                            </li>
                            <li
                                className={selectedTab === "spam" ? "active" : ""}
                                onClick={() => setSelectedTab("spam")}
                            >
                                <MdReport /> Spam <span className='count'>0</span>
                            </li>
                        </ul>
                    </div>

                    <div className='email-inbox'>
                        {/* Inbox Tab */}
                        {selectedTab === "inbox" && (
                            <>
                                <h2>Inbox Emails</h2>
                                {loadingInbox && <p>Loading inbox emails...</p>}
                                {errorInbox && <p style={{ color: 'red' }}>Error: {errorInbox}</p>}

                                <div className="email-list">
                                    {!loadingInbox && !errorInbox && inboxEmails.map((email, index) => (
                                        <div key={email.Id || index} className="email-card">
                                            <div className="email-left">
                                                <input type="checkbox" />
                                                <div className="avatar">
                                                    {email.fromEmail?.charAt(0) || "I"}
                                                </div>
                                                <div className="email-info">
                                                    <div className="sender">{email.fromEmail}</div>
                                                    <div className="subject">{email.subject}</div>
                                                    <div className="preview">{email.body?.substring(0, 60)}...</div>
                                                    {email.attachmentNames && (
                                                        <div className="attachments">Attachments: {email.attachmentNames}</div>
                                                    )}
                                                </div>
                                            </div>
                                            <div className="email-right">
                                                <span className="timestamp">
                                                    {new Date(email.receviedTime).toLocaleString()}
                                                </span>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </>
                        )}

                        {/* Sent Tab */}
                        {selectedTab === "sent" && (
                            <>
                                <h2>Sent Emails</h2>
                                {loadingSent && <p>Loading sent emails...</p>}
                                {errorSent && <p style={{ color: 'red' }}>Error: {errorSent}</p>}

                                <div className="email-list">
                                    {!loadingSent && !errorSent && sentEmails.map((email, index) => (
                                        <div key={email.Id || index} className="email-card">
                                            <div className="email-left">
                                                <input type="checkbox" />
                                                <div className="avatar">
                                                    {email.toEmail?.charAt(0) || "S"}
                                                </div>
                                                <div className="email-info">
                                                    <div className="sender">{email.toEmail}</div>
                                                    <div className="subject">{email.subject}</div>
                                                    <div className="preview">{email.body?.substring(0, 60)}...</div>
                                                    {email.attachmentNames && (
                                                        <div className="attachments">Attachments: {email.attachmentNames}</div>
                                                    )}
                                                </div>
                                            </div>
                                            <div className="email-right">
                                                <span className="timestamp">
                                                    {new Date(email.sentTime).toLocaleString()}
                                                </span>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </>
                        )}

                        {/* Other tabs placeholders */}
                        {selectedTab !== "sent" && selectedTab !== "inbox" && (
                            <h2>{selectedTab.charAt(0).toUpperCase() + selectedTab.slice(1)} Emails (placeholder)</h2>
                        )}
                    </div>
                </div>
            </div>
            <ComposeModal isOpen={isComposeOpen} onClose={() => setIsComposeOpen(false)} />
        </div>
    );
}

export default Inbox;
