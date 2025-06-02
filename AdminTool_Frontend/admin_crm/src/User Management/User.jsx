import React from 'react'
import Sidebar from '../Layout/Sidebar'
import Navbar from '../Layout/Navbar'
import '../Style/userManagement.css'

function User() {
  return (
    <div className='role-page'>
      <Navbar />
      <div className='main-layout'>
        <Sidebar />
      </div>
    </div>
  )
}

export default User