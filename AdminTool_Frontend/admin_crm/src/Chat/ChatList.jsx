import React, { useState } from 'react';
import Navbar from '../Layout/Navbar';
import Sidebar from '../Layout/Sidebar';
import '../Style/chat.css';

function ChatList() {
  const [searchTerm, setSearchTerm] = useState('');
  const [chats] = useState([
    { id: 1, name: 'Alice' },
    { id: 2, name: 'Bob' },
    { id: 3, name: 'Charlie' }
  ]);
  const [selectedChat, setSelectedChat] = useState(null);

  const filteredChats = chats.filter(chat =>
    chat.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

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
              {filteredChats.map(chat => (
                <li
                  key={chat.id}
                  className={`chat-item ${selectedChat?.id === chat.id ? 'active' : ''}`}
                  onClick={() => setSelectedChat(chat)}
                >
                  {chat.name}
                </li>
              ))}
            </ul>
          </div>

          <div className='chat-details'>
            {selectedChat ? (
              <>
                <h3>{selectedChat.name}</h3>
                <div className='chat-messages'>
                  {/* Mocked messages or messages fetched from backend */}
                  <p><strong>{selectedChat.name}:</strong> Hello!</p>
                  <p><strong>You:</strong> Hi, how are you?</p>
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

export default ChatList;
