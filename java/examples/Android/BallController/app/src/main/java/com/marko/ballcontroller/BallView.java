package com.marko.ballcontroller;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.support.annotation.Nullable;
import android.util.AttributeSet;
import android.view.View;

/**
 * Created by marko.tanic on 10.6.2017.
 */

public class BallView extends View {
    public float x, y, z;
    public int red = 0, green = 0, blue = 0, alpha = 255;

    public BallView(Context context) {
        super(context);
    }

    public BallView(Context context, @Nullable AttributeSet attrs) {
        super(context, attrs);
    }

    public BallView(Context context, @Nullable AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
    }

    @Override
    public void onDraw(Canvas c) {
        Paint p = new Paint();
        p.setARGB(alpha, red, green, blue);
        c.drawCircle(c.getWidth()/2 + x, c.getHeight() /2 - y, 50, p);
    }

}
