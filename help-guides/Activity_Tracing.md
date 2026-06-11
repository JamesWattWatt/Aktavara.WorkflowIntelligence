# Activity Tracing

**Activity Tracing** in Aktavara Console is a diagnostic feature used to monitor and capture internal client activity. It is primarily intended for troubleshooting, support cases, and analyzing unexpected behaviors in the Console. When enabled, Activity Tracing collects real-time operational logs and displays them directly inside the Console, allowing users or support teams to review what the client is doing at any given moment.

---

## Enabling Activity Tracing

1. Open **Aktavara Console**.  
2. From the ribbon, go to **View**, then click **Activity Tracing**.  
3. A dedicated tracing workspace appears, showing real-time client activity.

Once started, Console begins recording events immediately according to the current tracing settings.

---

## Tracing Toolbar Options

The tracing workspace includes several controls that manage how tracing behaves:

### **Start / Stop**

- Begins or pauses the capture of trace activity.  
- Useful for isolating specific operations or minimizing noise in the trace log.

### **Timeout**

- Sets how long tracing remains active, in minutes.  
- Tracing automatically stops once the timeout expires.

### **Enable at restart**

- Determines whether tracing should automatically re-enable when the Console restarts.  
- Useful for troubleshooting issues that occur during startup.

### **Export**

- Saves the current trace output to a **text file**.  
- Ideal for sending diagnostic information to support or archiving logs externally.

### **Clear Workspace**

- Empties the current trace window.  
- Does not stop active tracing—only clears the display.
