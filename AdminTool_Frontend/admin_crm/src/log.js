import React, { useEffect, useState } from 'react';
import { View, Text, ScrollView } from 'react-native';
import FileLogger from 'react-native-file-logger';

const Log = () => {
  const [logPaths, setLogPaths] = useState([]);

  useEffect(() => {
    // Initialize logger (if not done elsewhere)
    FileLogger.configure({
      captureConsole: true,
      dailyRolling: true,
      maximumFileSize: 1024 * 1024, // 1MB
      maximumNumberOfFiles: 5,
    });

    // Retrieve log file paths
    FileLogger.getLogFilePaths().then(paths => {
      console.log('Retrieved log file paths:', paths);
      setLogPaths(paths);
    });
  }, []);

  return (
    <ScrollView style={{ padding: 16 }}>
      <Text style={{ fontWeight: 'bold', fontSize: 18 }}>Log File Paths:</Text>
      {logPaths.map((path, index) => (
        <Text key={index} style={{ marginTop: 8 }}>{path}</Text>
      ))}
    </ScrollView>
  );
};

export default Log;
