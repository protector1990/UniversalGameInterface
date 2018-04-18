package com.marfilstudios.ugi;

import java.io.*;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

class TCPClient {

    private String serverIP; //your computer IP address
    private int serverPort;
    private OnMessageReceived mMessageListener = null;
    private boolean mRun = false;
    //create a socket to make the connection with the server
    Socket socket;
    private Thread tcpClientThread = new Thread(new Runnable() {
        @Override
        public void run() {
            try {
                //SocketAddress endpoint = InetSocketAddress.createUnresolved(serverIP, serverPort);
                InetAddress x = InetAddress.getByName(serverIP);
                InetSocketAddress a = new InetSocketAddress(x, serverPort);
                socket = new Socket();
                socket.connect(a);
            }
            catch (IOException e) {
                e.printStackTrace();
            }
            mRun = true;
            while (mRun) {
                try {
                    if (hasToSend) {
                        send();
                        hasToSend = false;
                    }
                } catch (Exception e) {
                    e.printStackTrace();
                }
                try {
                    listen();
                } catch (Exception e) {
                    e.printStackTrace();
				}
                try {
                    Thread.sleep(10);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
            }
            try {
                socket.close();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
    });

    ByteBuffer inBuffer = ByteBuffer.allocate(2048);
    ByteBuffer outBuffer = ByteBuffer.allocate(2048);

    public TCPClient(OnMessageReceived listener, String serverIP, int serverPort) {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        mMessageListener = listener;
    }

    public void start() {
        outBuffer.order(ByteOrder.LITTLE_ENDIAN);
        tcpClientThread.start();
    }

    public void stopClient(){
        mRun = false;
        //tcpClientThread.stop();
    }

    public void appendForSending(Object object) {
        if (object instanceof Float) {
            outBuffer.putFloat((Float)object);
            return;
        }
        if (object instanceof Integer) {
            outBuffer.putInt((Integer)object);
            return;
        }
        if (object instanceof String) {
            try {
                byte[] rawString = ((String)object).getBytes("US-ASCII");
                byte rawStringLength = (byte)rawString.length;
                outBuffer.put(rawStringLength);
                outBuffer.put(rawString);
            } catch (UnsupportedEncodingException e) {
                e.printStackTrace();
            }
            return;
        }
        if (object instanceof Boolean) {
            Boolean b = (Boolean)object;
            if (b) {
                outBuffer.put((byte)1);
            }
            else {
                outBuffer.put((byte)0);
            }
            return;
        }
        if (object instanceof Byte) {
            outBuffer.put((Byte)object);
            return;
        }
		//TODO: use some multiplatform logging solution, like slf4j
        System.out.println("Value not recognized");
    }

    public void appendNonLengthedStringForSending(String s) {
        try {
            byte[] rawString = s.getBytes("US-ASCII");
            outBuffer.put(rawString);
        } catch (UnsupportedEncodingException e) {
            e.printStackTrace();
        }
    }

    private void listen() {
        try {
            //in this while the client listens for the messages sent by the server
            if (socket.getInputStream().available() > 0) {
                int readBytes = socket.getInputStream().read(inBuffer.array());
                if (readBytes > 0 && mMessageListener != null) {
                    //call the method messageReceived from MyActivity class
                    mMessageListener.messageReceived(inBuffer);
                }
                inBuffer.clear();
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    private Boolean hasToSend = false;
    public void startSending() {
        hasToSend = true;
    }

    private void send() {
        try {
            socket.getOutputStream().write(outBuffer.array(), 0, outBuffer.position());
            socket.getOutputStream().flush();
            outBuffer.clear();
            hasToSend = false;
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public int getClientPort() {
        if (socket.isConnected()) {
            return socket.getLocalPort();
        }
        return 0;
    }

    //Declare the interface. The method messageReceived(String message) will must be implemented in the MyActivity
    //class at on asynckTask doInBackground
    public interface OnMessageReceived {
        public void messageReceived(ByteBuffer message);
    }
}