import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import '../Style/application.css'; // Optional CSS for styling

function OpenMail() {
    const { id } = useParams();
    const [email, setEmail] = useState(null);
    const [error, setError] = useState(null);

    useEffect(() => {
        fetch(`https://localhost:7210/api/Email/${id}`)
            .then(res => {
                if (!res.ok) throw new Error('Failed to fetch email');
                return res.json();
            })
            .then(data => {
                setEmail(data);
            })
            .catch(err => {
                setError(err.message);
            });
    }, [id]);

    if (error) return <p style={{ color: 'red' }}>Error: {error}</p>;
    if (!email) return <p>Loading...</p>;

    return (
        <div className='application-page'>
            <Navbar />
            <div className='main-layout'>
                <Sidebar />
                <div className='application-container'>
                    <h2>Email Details</h2>
                    
                    <div className="email-detail-card">
                        <h3>{email.subject}</h3>
                        <p><strong>From:</strong> {email.fromEmail}</p>
                        <p><strong>To:</strong> {email.toEmail}</p>
                        <p><strong>Received:</strong> {new Date(email.receviedTime).toLocaleString()}</p>
                        <div className="email-body">
                            <p>{email.body}</p>
                        </div>
                        {email.attachmentNames && (
                            <div className="attachments">
                                <strong>Attachments:</strong> {email.attachmentNames}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

export default OpenMail