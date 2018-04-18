package com.marfilstudios.ugi;

import java.io.UnsupportedEncodingException;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetSocketAddress;
import java.net.SocketException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.HashMap;

/**
 * Created by marko.tanic on 5.6.2017.
 */

class UDPClient {
    private HashMap<String, Method> streamHandlers = new HashMap<>();
    private HashMap<String, Object> streamHandlerObjects = new HashMap<>();
    private DatagramSocket socket;
    private Boolean started = false;
    private int port;

    public void start(int port) {
        this.port = port;
        clientThread.start();
    }

    public void stop() {
        started = false;
    }

    private Thread clientThread = new Thread(new Runnable() {
        @Override
        public void run() {
			//TODO: use some multiplatform logging solution, like slf4j
            System.out.println("Thread " + Thread.currentThread().getName() + " started");
            try {
                socket = new DatagramSocket(null);
                socket.bind(new InetSocketAddress(port));
                socket.setSoTimeout(500);
				//TODO: use some multiplatform logging solution, like slf4j
                System.out.println("Socket started on port " + Integer.toString(port));
            } catch (SocketException e) {
                e.printStackTrace();
                return;
            }
            inBuff.order(ByteOrder.LITTLE_ENDIAN);
            started = true;

            while (started) {
                try {
                    handleStreams();
                } catch (Exception e) {
                   e.printStackTrace();
                }
            }
            socket.close();
        }
    });


    private ByteBuffer inBuff = ByteBuffer.allocate(2048);
    public void handleStreams() {
        inBuff.position(0);
        DatagramPacket receivedPacket = new DatagramPacket(inBuff.array(), 2048);
        try {
            socket.receive(receivedPacket);
        } catch (Exception e) {
            e.printStackTrace();
            return;
        }
        if (receivedPacket.getLength() == 0) {
            return;
        }
        byte nameLength = inBuff.get();
        String streamName = null;
        try {
            streamName = new String(inBuff.array(), 1, nameLength, "US-ASCII");
        } catch (Exception e) {
            e.printStackTrace();
            return;
        }
        inBuff.position(nameLength + 1);
        if (streamHandlers == null) {
            return;
        }
        Method handler = streamHandlers.get(streamName);
        if (handler == null) {
            return;
        }
        Class<?>[] paramTypes = handler.getParameterTypes();
        Object[] params = new Object[paramTypes.length];
        int i = 0;
        try {
            for (Class c : paramTypes) {
                if (c.equals(String.class)) {
                    byte strLength = inBuff.get();
                    String s = null;
                    try {
                        s = new String(inBuff.array(), inBuff.position(), nameLength, "US-ASCII");
                    } catch (UnsupportedEncodingException e) {
                        e.printStackTrace();
                    }
                    inBuff.position(inBuff.position() + strLength);
                    params[i] = s;
                    ++i;
                    continue;
                }
                if (c.equals(Float.class)) {
                    params[i] = inBuff.getFloat();
                    ++i;
                    continue;
                }
                if (c.equals(Integer.class)) {
                    params[i] = inBuff.getInt();
                    ++i;
                    continue;
                }
                if (c.equals(Boolean.class)) {
                    byte bool = inBuff.get();
                    Boolean b = true;
                    if (bool == 0) {
                        b = false;
                    }
                    params[i] = b;
                    ++i;
                    continue;
                }
            }
        } catch (Exception e) {
            e.printStackTrace();
            return;
        }
        try {
            if (streamHandlerObjects == null) {
                return;
            }
            Object o = streamHandlerObjects.get(streamName);
            if (o == null) {
                return;
            }
            handler.invoke(o, params);
        } catch (IllegalAccessException e) {
            System.out.println(e.getMessage());
        } catch (InvocationTargetException e) {
            System.out.println(e.toString());
        }
        catch (Exception e) {
            System.out.println(e.getMessage());
        }
    }

    public void registerStream(Method handler, Object receivingObject, String streamName) {
        streamHandlers.put(streamName, handler);
        streamHandlerObjects.put(streamName, receivingObject);
    }
}
