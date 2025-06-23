import React, { useEffect, useState } from 'react';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import '../Style/chat.css';
import axios from 'axios';

function Chat() {
  const [searchTerm, setSearchTerm] = useState('');
  const [users, setUsers] = useState([]);
  const [selectedChat, setSelectedChat] = useState(null);
  const [message, setMessage] = useState('');
  const [messages, setMessages] = useState([]);

  const currentUser = 'You'; // Replace with logged-in user name

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const res = await axios.get(`https://localhost:7210/api/Chat/Users?currentUser=${currentUser}`);
        setUsers(res.data);
      } catch (err) {
        console.error('Error fetching users:', err);
      }
    };

    fetchUsers();
  }, [currentUser]);

  const filteredUsers = users.filter(user =>
    user.username.toLowerCase().includes(searchTerm.toLowerCase())
  );

 const handleSendMessage = async () => {
  if (!message.trim() || !selectedChat) return;

  const newMessage = {
    sender: currentUser,
    receiver: selectedChat.username,
    text: message.trim()
  };

  setMessages(prev => [...prev, newMessage]);
  setMessage('');

  try {
    await axios.post('https://localhost:7210/api/Chat/SendMessage', {
      senderUsername: currentUser,
      receiverUsername: selectedChat.username,
      messageText: message.trim()
    });
  } catch (err) {
    console.error('Error sending message:', err);
  }
};


  return (
    <div className='chat-page'>
      <Navbar />
      <div className='main-layout'>
        <Sidebar />
        <div className='chat-container'>
          <div className='chat-sidebar'>
            <input
              type='text'
              placeholder='Search users...'
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className='search-input'
            />
            <ul className='chat-list'>
              {filteredUsers.map(user => (
                <li
                  key={user.id}
                  className={`chat-item ${selectedChat?.id === user.id ? 'active' : ''}`}
                  onClick={() => {
                    setSelectedChat(user);
                    setMessages([]); // Optional: Reset messages on new chat
                  }}
                >
                  {user.username}
                </li>
              ))}
            </ul>
          </div>

          <div className='chat-details'>
            {selectedChat ? (
              <>
                <h3>{selectedChat.username}</h3>
                <div className='chat-messages'>
                  {messages.map((msg, index) => (
                    <p key={index}>
                      <strong>{msg.sender}:</strong> {msg.text}
                    </p>
                  ))}
                </div>
                <div className='chat-input-section'>
                  <input
                    type='text'
                    placeholder='Type your message...'
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    className='chat-message-input'
                  />
                  <button onClick={handleSendMessage} className='chat-send-button'>
                    Send
                  </button>
                </div>
              </>
            ) : (
              <p>Select a chat to start messaging</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default Chat;
