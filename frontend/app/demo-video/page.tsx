import Video from 'next-video';
import sampleVideo from '@videos/sample-video.mp4';

export default function DemoVideo() {
  return (
    <div style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      minHeight: '100vh',
      backgroundColor: '#f5f5f5',
      padding: '20px'
    }}>
      <div style={{
        maxWidth: '800px',
        width: '100%',
        backgroundColor: 'white',
        borderRadius: '8px',
        padding: '20px',
        boxShadow: '0 2px 10px rgba(0,0,0,0.1)'
      }}>
        <h1 style={{
          textAlign: 'center',
          marginBottom: '20px',
          color: '#333'
        }}>
          Next-Video Demo
        </h1>
        <Video
          src={sampleVideo}
          style={{
            width: '100%',
            height: 'auto',
            borderRadius: '4px'
          }}
        />
        <p style={{
          textAlign: 'center',
          marginTop: '20px',
          color: '#666',
          fontSize: '14px'
        }}>
          If you can see this video playing, your next-video setup is working correctly!
        </p>
      </div>
    </div>
  );
}
