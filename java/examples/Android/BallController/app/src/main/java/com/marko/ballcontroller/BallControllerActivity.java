package com.marko.ballcontroller;

import android.os.Handler;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Spinner;

import com.marfilstudios.ugi.Client;

public class BallControllerActivity extends AppCompatActivity implements AdapterView.OnItemSelectedListener {

    private Spinner spinner;
    private static final String[]colors = {"Black", "Red", "Green", "Blue"};

    private BallView ballView;
    private Handler h;
    private BallDrawer ballDrawer;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_ball_controller);

        spinner = (Spinner)findViewById(R.id.BallColorChooser);
        ArrayAdapter<String> adapter = new ArrayAdapter<String>(BallControllerActivity.this,
                android.R.layout.simple_spinner_item,colors);

        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spinner.setAdapter(adapter);
        spinner.setOnItemSelectedListener(this);

        ballView = (BallView) findViewById(R.id.BallView);
        ballDrawer = new BallDrawer();
        //setContentView(ballView);
        h = new Handler();
        try {
            Client.Instance().subscribeToStream("Ball", this, this.getClass().getMethod("HandleDrawStream", Float.class, Float.class, Float.class));
        } catch (NoSuchMethodException e) {
            Log.e("BallDrawer", "No handler method");
            Log.wtf(e.getMessage(), e);
        }
    }

    @Override
    public void onItemSelected(AdapterView<?> parent, View v, int position, long id) {

        switch (position) {
            case 0:
                Client.Instance().sendCommand("SetBallColor", 0f, 0f, 0f, 1f);
                ballDrawer.red = 0;
                ballDrawer.green = 0;
                ballDrawer.blue = 0;
                ballDrawer.alpha = 255;
                break;
            case 1:
                Client.Instance().sendCommand("SetBallColor", 1f, 0f, 0f, 1f);
                ballDrawer.red = 255;
                ballDrawer.green = 0;
                ballDrawer.blue = 0;
                ballDrawer.alpha = 255;
                break;
            case 2:
                Client.Instance().sendCommand("SetBallColor", 0f, 1f, 0f, 1f);
                ballDrawer.red = 0;
                ballDrawer.green = 255;
                ballDrawer.blue = 0;
                ballDrawer.alpha = 255;
                break;
            case 3:
                Client.Instance().sendCommand("SetBallColor", 0f, 0f, 1f, 1f);
                ballDrawer.red = 0;
                ballDrawer.green = 0;
                ballDrawer.blue = 255;
                ballDrawer.alpha = 255;
                break;
        }
    }

    @Override
    public void onNothingSelected(AdapterView<?> parent) {

    }

    public void HandleDrawStream(Float x, Float y, Float z) {
        ballDrawer.x = x;
        ballDrawer.y = y;
        ballDrawer.z = z;
        h.post(ballDrawer);
    }


    class BallDrawer implements Runnable {
        public float x, y, z;
        public int red = 0, green = 0, blue = 0, alpha = 255;

        @Override
        public void run() {
            ballView.x = x * 200;
            ballView.y = y * 200;
            ballView.z = z;
            ballView.red = red;
            ballView.blue = blue;
            ballView.green = green;
            ballView.alpha = alpha;
            ballView.invalidate();
        }
    }

}
