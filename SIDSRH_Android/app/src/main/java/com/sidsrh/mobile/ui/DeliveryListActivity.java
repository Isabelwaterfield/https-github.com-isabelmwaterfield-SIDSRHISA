package com.sidsrh.mobile.ui;

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.sidsrh.mobile.data.ApiClient;
import java.util.concurrent.Executors;

public class DeliveryListActivity extends AppCompatActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.setPadding(32, 32, 32, 32);
        content.addView(Ui.title(this, "Deliveries"));
        content.addView(Ui.body(this, "Tap a delivery to see full details and confirm codes."));

        LinearLayout list = new LinearLayout(this);
        list.setOrientation(LinearLayout.VERTICAL);
        content.addView(list);

        setContentView(Ui.screenWithBottomBack(this, content, this::finish));

        String token = getSharedPreferences("sidsrh", MODE_PRIVATE).getString("token", "");
        Executors.newSingleThreadExecutor().execute(() -> {
            try {
                String body = ApiClient.get("api/Deliveries", token);
                JsonArray arr = JsonParser.parseString(body).getAsJsonArray();
                new Handler(Looper.getMainLooper()).post(() -> {
                    list.removeAllViews();
                    if (arr.size() == 0) {
                        list.addView(Ui.body(this, "No deliveries. Create one on the web app first."));
                        return;
                    }
                    for (JsonElement el : arr) {
                        JsonObject o = el.getAsJsonObject();
                        String id = o.get("deliveryId").getAsString();
                        String status = o.get("deliveryStatus").getAsString();
                        String recipient = o.has("recipientName") && !o.get("recipientName").isJsonNull()
                                ? o.get("recipientName").getAsString() : "";
                        String km = o.has("distanceKm") ? String.valueOf(o.get("distanceKm").getAsDouble()) : "";
                        Button row = new Button(this);
                        row.setAllCaps(false);
                        row.setText(id + "\n" + status + " · " + km + " km\n" + recipient);
                        row.setOnClickListener(v -> {
                            Intent i = new Intent(this, ConfirmActivity.class);
                            i.putExtra("deliveryId", id);
                            startActivity(i);
                        });
                        list.addView(row);
                    }
                });
            } catch (Exception e) {
                new Handler(Looper.getMainLooper()).post(() ->
                        Toast.makeText(this, "Cannot load deliveries. Is API running?\n" + e.getMessage(), Toast.LENGTH_LONG).show());
            }
        });
    }
}
