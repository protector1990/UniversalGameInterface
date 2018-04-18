package com.marfilstudios.ugi;

import java.lang.reflect.Method;
import java.util.HashMap;

/**
 * Created by marko.tanic on 5.6.2017.
 */

public class Client {

    private static Client instance;

    synchronized public static Client Instance() {
        if (instance == null) {
            instance = new Client();
        }
        return instance;
    }

    private Client() {

    }

    private HashMap<String, Integer> commands;

    public HashMap<String, Integer> getCommands() {
        return commands;
    }

    public void setCommands(HashMap<String, Integer> commands) {
        this.commands = commands;
    }

    private TCPClient tcpClient;
    private UDPClient udpClient;
    private Boolean tcpStarted = false;
    private Boolean started = false;

    public Object lock = new Object();

    synchronized public void start(String serverIP, int port, String secret) {
        tcpClient = new TCPClient(new MessageListener(), serverIP, port);
        tcpClient.start();
        tcpClient.appendNonLengthedStringForSending(secret);
        tcpClient.startSending();

        synchronized (lock) {
            while (!tcpStarted) {
                try {
                    lock.wait();
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
            }
        }
        udpClient = new UDPClient();
        udpClient.start(tcpClient.getClientPort());
        started = true;
    }

    /**
     * Serves as the callback for notifying that the tcpClient has been successfully started.
     */
    void tcpStarted() {
        synchronized (lock) {
            tcpStarted = true;
            lock.notify();
            tcpClient.appendForSending((byte) 4);
            tcpClient.startSending();
        }
    }

    public Boolean isStarted() {
        return started;
    }

    public void sendCommand(String name, Object ...params) {
        Integer commandId = commands.get(name);
        if (commandId != null) {
            tcpClient.appendForSending((byte)3);
            tcpClient.appendForSending(commandId.byteValue());
            for (Object o : params) {
                tcpClient.appendForSending(o);
            }
            tcpClient.startSending();
        }
    }

    public void subscribeToStream(String streamName, Object receivingObject, Method handler) {
        udpClient.registerStream(handler, receivingObject, streamName);
        tcpClient.appendForSending((byte)1);
        tcpClient.appendForSending(streamName);
        tcpClient.startSending();
    }
}
