package com.marko.ballcontroller;

import android.content.Intent;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.EditText;

import com.marfilstudios.ugi.Client;

public class ConnectActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_connect);
        Thread.setDefaultUncaughtExceptionHandler(new Thread.UncaughtExceptionHandler() {
            @Override
            public void uncaughtException(Thread t, Throwable e) {
                Log.e(Thread.currentThread().getName(), "Uncaught exception: " + e.toString());
                Log.wtf(e.getLocalizedMessage(), e);
            }
        });
    }

    public void OnConnect(View v) {
        EditText ipEdit = ((EditText) findViewById(R.id.IPAddressText));
        EditText portEdit = (EditText) findViewById(R.id.PortText);
        String ip = ipEdit.getText().toString();
        int port = Integer.parseInt(portEdit.getText().toString());
        Client.Instance().start(ip, port, "marva");
        try {
            Thread.sleep(1500);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
        if (Client.Instance().isStarted()) {
            Intent intent = new Intent(this, BallControllerActivity.class);
            startActivity(intent);
        }
    }
}
