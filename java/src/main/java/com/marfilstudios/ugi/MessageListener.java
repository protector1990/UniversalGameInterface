package com.marfilstudios.ugi;

import java.io.UnsupportedEncodingException;
import java.nio.ByteBuffer;
import java.util.HashMap;

/**
 * Created by marko.tanic on 5.6.2017.
 */

class MessageListener implements TCPClient.OnMessageReceived {
    @Override
    synchronized public void messageReceived(ByteBuffer message) {
        byte messageOperaiton = message.get();
        switch (messageOperaiton) {
            case 0: {
                //get available commands
                HashMap<String, Integer> commands = new HashMap<>();
                byte commandsNum = message.get();
                for (int i = 0; i < commandsNum; ++i) {
                    int commandNameLength = (int)message.get();
                    byte[] rawCommandName = new byte[commandNameLength];
                    message.get(rawCommandName, 0, commandNameLength);
                    String s = null;
                    try {
                        s = new String(rawCommandName, "US-ASCII");
                        commands.put(s, i);
                    } catch (UnsupportedEncodingException e) {
                        e.printStackTrace();
                    }
                }
                Client.Instance().setCommands(commands);
                break;
            }
            case 1: {
                Client.Instance().tcpStarted();
                break;
            }
        }
    }
}
