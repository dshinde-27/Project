import React, { useState, useEffect } from 'react';
import '../Style/application.css';
import {
    MdAttachFile,
    MdInsertPhoto,
    MdEmojiEmotions,
    MdLink,
    MdDelete,
    MdSend
} from 'react-icons/md';

const ComposeModal = ({ isOpen, onClose }) => {
    const [from, setFrom] = useState('');
    const [to, setTo] = useState('');
    const [subject, setSubject] = useState('');
    const [body, setBody] = useState('');
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const email = localStorage.getItem('email');
        setFrom(email || 'unknown@gmail.com');
    }, []);

    const handleSend = async () => {
        if (!to || !subject || !body) {
            alert("Please fill all fields.");
            return;
        }

        const emailData = {
            from,
            to,
            subject,
            body
        };

        setLoading(true);
        try {
            const res = await fetch('https://localhost:7210/api/Email/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(emailData)
            });

            if (res.ok) {
                alert('Email sent successfully!');
                // Clear form and close
                setTo('');
                setSubject('');
                setBody('');
                onClose();
            } else {
                const errorText = await res.text();
                alert(`Failed to send email. ${errorText}`);
            }
        } catch (err) {
            console.error(err);
            alert('Error sending email.');
        } finally {
            setLoading(false);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            <div className="modal">
                <div className="modal-header">
                    <h3>Compose New Email</h3>
                    <button onClick={onClose} className="close-btn">×</button>
                </div>

                <div className="modal-body">
                    <div className="email-info">
                        <div><strong>From:</strong> {from}</div>
                    </div>

                    <input 
                        type="email" 
                        placeholder="To" 
                        value={to} 
                        onChange={(e) => setTo(e.target.value)} 
                        className="input" 
                        required 
                    />

                    <input 
                        type="text" 
                        placeholder="Subject" 
                        value={subject}
                        onChange={(e) => setSubject(e.target.value)}
                        className="input" 
                        required 
                    />

                    <textarea 
                        placeholder="Compose your message..."
                        value={body}
                        onChange={(e) => setBody(e.target.value)}
                        className="textarea"
                        required 
                    />

                    <div className="toolbar">
                        <div className="icon-buttons">
                            <MdAttachFile size={20} title="Attach File" />
                            <MdInsertPhoto size={20} title="Insert Photo" />
                            <MdEmojiEmotions size={20} title="Emoji" />
                            <MdLink size={20} title="Insert Link" />
                        </div>
                        <div className="actions">
                            <button className="icon-btn" onClick={() => {
                                setTo('');
                                setSubject('');
                                setBody('');
                            }}>
                                <MdDelete size={20} />
                            </button>
                            <button className="send-btn" onClick={handleSend} disabled={loading}>
                                {loading ? 'Sending...' : 'Send'} <MdSend size={18} />
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ComposeModal;
