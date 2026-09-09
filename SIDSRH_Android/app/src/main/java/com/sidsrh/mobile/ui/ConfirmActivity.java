package com.sidsrh.mobile.ui;

import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.sidsrh.mobile.data.ApiClient;
import java.util.concurrent.Executors;

public class ConfirmActivity extends AppCompatActivity {
    private TextView detail;
    private EditText idBox, codeBox;
    private String token;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        token = getSharedPreferences("sidsrh", MODE_PRIVATE).getString("token", "");
        String preId = getIntent().getStringExtra("deliveryId");

        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.setPadding(32, 32, 32, 32);
        content.addView(Ui.title(this, "Confirm delivery"));
        content.addView(Ui.body(this,
                "1) Load delivery to see full details\n" +
                "2) Enter DISPATCH code when package is leaving\n" +
                "3) After landing, enter ARRIVAL code (different code)\n" +
                "API must be running — same database as website."));

        idBox = new EditText(this);
        idBox.setHint("Delivery ID e.g. DEL001");
        if (preId != null) idBox.setText(preId);
        content.addView(idBox);

        Button load = new Button(this);
        load.setText("Load full details");
        load.setOnClickListener(v -> loadDetails());
        content.addView(load);

        detail = new TextView(this);
        detail.setText("Details will appear here after Load.");
        detail.setTextIsSelectable(true);
        detail.setPadding(0, 16, 0, 16);
        content.addView(detail);

        codeBox = new EditText(this);
        codeBox.setHint("Enter code from details above");
        content.addView(codeBox);

        Button dispatch = new Button(this);
        dispatch.setText("Confirm DISPATCH (package leaving)");
        dispatch.setOnClickListener(v -> call("confirm-dispatch"));
        content.addView(dispatch);

        Button arrive = new Button(this);
        arrive.setText("Confirm ARRIVAL (unlock parcel)");
        arrive.setOnClickListener(v -> call("confirm-arrival"));
        content.addView(arrive);

        Button demo = new Button(this);
        demo.setText("Demo: mark package landed");
        demo.setOnClickListener(v -> demoArrive());
        content.addView(demo);

        setContentView(Ui.screenWithBottomBack(this, content, this::finish));

        if (preId != null && !preId.isEmpty()) loadDetails();
    }

    private void loadDetails() {
        String id = idBox.getText().toString().trim();
        if (id.isEmpty()) {
            Toast.makeText(this, "Enter a delivery ID", Toast.LENGTH_SHORT).show();
            return;
        }
        detail.setText("Loading…");
        Executors.newSingleThreadExecutor().execute(() -> {
            try {
                String body = ApiClient.get("api/Deliveries", token);
                JsonArray arr = JsonParser.parseString(body).getAsJsonArray();
                JsonObject found = null;
                for (JsonElement el : arr) {
                    JsonObject o = el.getAsJsonObject();
                    if (id.equalsIgnoreCase(o.get("deliveryId").getAsString())) {
                        found = o;
                        break;
                    }
                }
                final JsonObject o = found;
                new Handler(Looper.getMainLooper()).post(() -> {
                    if (o == null) {
                        detail.setText("Delivery not found: " + id + "\nCheck the ID on the website Deliveries page.");
                        return;
                    }
                    String text =
                            "DELIVERY DETAILS\n" +
                            "────────────────\n" +
                            "ID: " + g(o, "deliveryId") + "\n" +
                            "Status: " + g(o, "deliveryStatus") + "\n" +
                            "Service: " + g(o, "serviceClass") + "\n" +
                            "Distance: " + g(o, "distanceKm") + " km\n" +
                            "Cost: R " + g(o, "calculatedCost") + "\n" +
                            "Recipient: " + g(o, "recipientName") + "\n" +
                            "Phone: " + g(o, "recipientPhone") + "\n" +
                            "Drone: " + g(o, "droneId") + "\n" +
                            "Clinic: " + g(o, "clinicId") + "\n" +
                            "Supply: " + g(o, "supplyId") + "\n" +
                            "Origin: " + g(o, "originLocationId") + "\n" +
                            "Destination: " + g(o, "destinationLocationId") + "\n\n" +
                            "CODES\n" +
                            "Dispatch (leaving): " + g(o, "dispatchCode") + "\n" +
                            "Arrival (unlock):   " + g(o, "arrivalCode") + "\n" +
                            "ETA: " + g(o, "estimatedArrival") + "\n\n" +
                            "Use the matching code below for the current step.";
                    detail.setText(text);
                    // prefill code based on status
                    String st = g(o, "deliveryStatus");
                    if ("Assigned".equalsIgnoreCase(st) || "Pending".equalsIgnoreCase(st))
                        codeBox.setText(g(o, "dispatchCode"));
                    else if ("In Transit".equalsIgnoreCase(st))
                        codeBox.setText(g(o, "arrivalCode"));
                });
            } catch (Exception e) {
                new Handler(Looper.getMainLooper()).post(() ->
                        detail.setText("Could not load.\nIs the API running at " + ApiClient.baseUrl() + "?\n" + e.getMessage()));
            }
        });
    }

    private static String g(JsonObject o, String k) {
        if (!o.has(k) || o.get(k).isJsonNull()) return "—";
        return o.get(k).getAsString();
    }

    private void call(String action) {
        String id = idBox.getText().toString().trim();
        String code = codeBox.getText().toString().trim();
        if (id.isEmpty() || code.isEmpty()) {
            Toast.makeText(this, "Delivery ID and code required", Toast.LENGTH_SHORT).show();
            return;
        }
        Executors.newSingleThreadExecutor().execute(() -> {
            try {
                String json = "{\"code\":\"" + code + "\"}";
                String body = ApiClient.post("api/Deliveries/" + id + "/" + action, json, token);
                new Handler(Looper.getMainLooper()).post(() -> {
                    Toast.makeText(this, "OK", Toast.LENGTH_SHORT).show();
                    detail.setText(detail.getText() + "\n\nRESULT:\n" + body);
                    loadDetails();
                });
            } catch (Exception e) {
                new Handler(Looper.getMainLooper()).post(() ->
                        Toast.makeText(this, e.getMessage(), Toast.LENGTH_LONG).show());
            }
        });
    }

    private void demoArrive() {
        String id = idBox.getText().toString().trim();
        Executors.newSingleThreadExecutor().execute(() -> {
            try {
                ApiClient.post("api/Deliveries/" + id + "/demo-arrive", "{}", token);
                new Handler(Looper.getMainLooper()).post(() -> {
                    Toast.makeText(this, "ETA set to now — use ARRIVAL code", Toast.LENGTH_SHORT).show();
                    loadDetails();
                });
            } catch (Exception e) {
                new Handler(Looper.getMainLooper()).post(() ->
                        Toast.makeText(this, e.getMessage(), Toast.LENGTH_LONG).show());
            }
        });
    }
}
