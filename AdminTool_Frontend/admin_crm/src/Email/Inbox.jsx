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

    const [sentEmails, setSentEmails] = useState([]);
    const [loadingSent, setLoadingSent] = useState(false);
    const [errorSent, setErrorSent] = useState(null);

    const [inboxEmails, setInboxEmails] = useState([]);
    const [loadingInbox, setLoadingInbox] = useState(false);
    const [errorInbox, setErrorInbox] = useState(null);

    const [selectedTab, setSelectedTab] = useState("inbox");
    const [selectedEmail, setSelectedEmail] = useState(null);

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
                            <li className={selectedTab === "inbox" ? "active" : ""} onClick={() => { setSelectedTab("inbox"); setSelectedEmail(null); }}>
                                <MdInbox /> Inbox <span className='count'>{inboxEmails.length}</span>
                            </li>
                            <li className={selectedTab === "starred" ? "active" : ""} onClick={() => { setSelectedTab("starred"); setSelectedEmail(null); }}>
                                <MdOutlineStar /> Starred <span className='count'>46</span>
                            </li>
                            <li className={selectedTab === "sent" ? "active" : ""} onClick={() => { setSelectedTab("sent"); setSelectedEmail(null); }}>
                                <MdSend /> Sent <span className='count'>{sentEmails.length}</span>
                            </li>
                            <li className={selectedTab === "drafts" ? "active" : ""} onClick={() => { setSelectedTab("drafts"); setSelectedEmail(null); }}>
                                <MdDrafts /> Drafts <span className='count'>12</span>
                            </li>
                            <li className={selectedTab === "deleted" ? "active" : ""} onClick={() => { setSelectedTab("deleted"); setSelectedEmail(null); }}>
                                <MdDelete /> Deleted <span className='count'>8</span>
                            </li>
                            <li className={selectedTab === "spam" ? "active" : ""} onClick={() => { setSelectedTab("spam"); setSelectedEmail(null); }}>
                                <MdReport /> Spam <span className='count'>0</span>
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
                                <p>
                                    <strong>{selectedTab === "inbox" ? "Received" : "Sent"}:</strong>{" "}
                                    {new Date(selectedEmail.receviedTime || selectedEmail.sentTime).toLocaleString()}
                                </p>
                                <div className="email-body">
                                    <p>{selectedEmail.body}</p>
                                </div>
                                {selectedEmail.attachmentNames && (
                                    <div className="attachments">
                                        <strong>Attachments:</strong> {selectedEmail.attachmentNames}
                                    </div>
                                )}
                            </div>
                        ) : (
                            <>
                                <h2>{selectedTab.charAt(0).toUpperCase() + selectedTab.slice(1)} Emails</h2>
                                {(selectedTab === "inbox" && loadingInbox) || (selectedTab === "sent" && loadingSent) ? (
                                    <p>Loading emails...</p>
                                ) : (selectedTab === "inbox" && errorInbox) || (selectedTab === "sent" && errorSent) ? (
                                    <p style={{ color: 'red' }}>Error: {errorInbox || errorSent}</p>
                                ) : (
                                    <div className="email-list">
                                        {(selectedTab === "inbox" ? inboxEmails : sentEmails).map((email, index) => (
                                            <div key={email.Id || index} className="email-card" onClick={() => setSelectedEmail(email)}>
                                                <div className="email-left">
                                                    <input type="checkbox" />
                                                    <div className="avatar">
                                                        {(selectedTab === "inbox" ? email.fromEmail : email.toEmail)?.charAt(0) || "?"}
                                                    </div>
                                                    <div className="email-info">
                                                        <div className="sender">{selectedTab === "inbox" ? email.fromEmail : email.toEmail}</div>
                                                        <div className="subject">{email.subject}</div>
                                                        <div className="preview">{email.body?.substring(0, 60)}...</div>
                                                        {email.attachmentNames && (
                                                            <div className="attachments">Attachments: {email.attachmentNames}</div>
                                                        )}
                                                    </div>
                                                </div>
                                                <div className="email-right">
                                                    <span className="timestamp">
                                                        {new Date(email.receviedTime || email.sentTime).toLocaleString()}
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
