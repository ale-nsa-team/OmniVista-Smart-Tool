#!/bin/python3

#There is no good way to map MAC address to IP address in general.
#
#This script will first fetch valid IP interfaces on the switch and calculate the known subnets.
#We then skip all Loopback and EMP  IP interfaces.
#
#Using the lowest priority queue on the switch, we ping every possible IP on the subnet.
#ARP cache by default is only 5 mins.  This means the script can't run longer than that.
#
import os
import subprocess
import threading
import queue
import time
import signal
import sys
import ipaddress
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor

# -------------------- Logging Setup --------------------
LOG_FILE = "console.log"
# Remove old log file if it exists
if os.path.exists(LOG_FILE):
    os.remove(LOG_FILE)

# Open the log file in write mode with line buffering.
log_file_handle = open(LOG_FILE, "w", buffering=1)

class Logger:
    def __init__(self, terminal, log):
        #self.terminal = terminal
        self.log = log
        self.buffer = ""

    def write(self, message):
        self.buffer += message
        while "\n" in self.buffer:
            line, self.buffer = self.buffer.split("\n", 1)
            timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
            formatted_line = f"[{timestamp}] {line}\n"
            #self.terminal.write(formatted_line)
            self.log.write(formatted_line)

    def flush(self):
        #self.terminal.flush()
        self.log.flush()

# Redirect stdout and stderr to the Logger.
sys.stdout = Logger(sys.stdout, log_file_handle)
sys.stderr = Logger(sys.stderr, log_file_handle)
# -------------------------------------------------------

LARGE_SUBNET_WARNING = 22  # Flag subnets larger than /22
MAX_CONCURRENT_PINGS = 256  # Number of parallel pings allowed
STATUS_INTERVAL = 30  # Seconds between status updates

# Set lowest possible priority! We can not bog the switch down.
os.nice(19)

# Global state
interfaces = []
subnet_ip_blocks = queue.Queue()
arp_table = {}  # Dictionary to store {IP: MAC}
subnet_priority = defaultdict(int)  # Tracks subnet popularity from ARP table
mac_lock = threading.Lock()
stop_threads = threading.Event()

# Gracefully handle Ctrl+C (^C)
def handle_exit(sig, frame):
    print("\n[!] Caught Ctrl+C, exiting cleanly...")
    stop_threads.set()  # Stop threads
    sys.exit(0)

signal.signal(signal.SIGINT, handle_exit)

# Run "show arp" and analyze the most populated subnets
def analyze_arp_table():
    print("[+] Running 'show arp' to determine priority subnets...")
    try:
        result = subprocess.run(["show", "arp"], capture_output=True, text=True)
        arp_entries = result.stdout.splitlines()

        for entry in arp_entries:
            parts = entry.split()
            if len(parts) >= 3:
                ip = parts[0]
                if ip.count('.') == 3:
                    subnet = str(ipaddress.IPv4Network(f"{ip}/24", strict=False))
                    subnet_priority[subnet] += 1  # Count occurrences per subnet

        if subnet_priority:
            print("\n[+] Most populated subnets (based on ARP table):")
            for subnet, count in sorted(subnet_priority.items(), key=lambda x: x[1], reverse=True):
                print(f"    {subnet} - {count} devices")

    except Exception as e:
        print(f"[!] Error retrieving ARP table: {e}")

# Run "show ip interface" and parse output
def get_interfaces():
    try:
        print("[+] Running 'show ip interface'...")
        result = subprocess.run(["show", "ip", "interface"], capture_output=True, text=True)

        lines = result.stdout.splitlines()
        for line in lines:
            parts = line.split()
            if len(parts) >= 6 and parts[1].count(".") == 3:  # Ensure it contains an IP
                name, ip, subnet, status, _, device = parts[:6]

                # Exclude Loopback and EMP-C interfaces
                if name.startswith(("Loopback", "EMP-C")):
                    print(f"[INFO] Skipping {name} (excluded by rule)")
                    continue

                # Check subnet size and warn if too large
                prefix_length = ipaddress.IPv4Network(f"{ip}/{subnet}", strict=False).prefixlen
                if prefix_length < LARGE_SUBNET_WARNING:
                    print(f"[WARNING] Skipping {name} ({ip}/{subnet}) - Subnet too large (/<{LARGE_SUBNET_WARNING})")
                    continue  # Skip subnets larger than /22

                interfaces.append({"name": name, "ip": ip, "subnet": subnet, "status": status, "device": device})
        return interfaces

    except Exception as e:
        print(f"[!] Error retrieving interfaces: {e}")
        sys.exit(1)

# Generate target IPs per subnet and enqueue them
def generate_ip_list():
    print("\n[+] Subnets to be scanned (prioritized by ARP table):")

    prioritized_interfaces = sorted(
        interfaces, key=lambda x: subnet_priority.get(str(ipaddress.IPv4Network(f"{x['ip']}/24", strict=False)), 0),
        reverse=True
    )

    for iface in prioritized_interfaces:
        if iface["status"] == "UP" and iface["ip"] != "0.0.0.0":  # Ignore down interfaces
            network = ipaddress.IPv4Network(f"{iface['ip']}/{iface['subnet']}", strict=False)

            print(f"    {iface['name']} - {network}")

            for ip in network.hosts():
                subnet_ip_blocks.put(str(ip))  # Convert to string and queue IPs

    print(f"\n[+] Loaded {subnet_ip_blocks.qsize()} IPs for scanning.")

# Function to ping an IP
def ping_ip(ip):
    if stop_threads.is_set():
        return
    proc = subprocess.Popen(["ping", ip, "count", "1", "timeout", "1"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc.wait()
    if proc.returncode == 0:
        print(f"[UP] {ip}")

# Function to ping all IPs using a thread pool
def ping_all_ips():
    print(f"\n[+] Starting fast ping scan with {MAX_CONCURRENT_PINGS} workers...")
    with ThreadPoolExecutor(max_workers=MAX_CONCURRENT_PINGS) as executor:
        while not subnet_ip_blocks.empty():
            ip = subnet_ip_blocks.get()
            executor.submit(ping_ip, ip)

# Main execution
if __name__ == "__main__":
    print("[*] Starting network discovery...\n")

    # Get ARP-based priority before scanning
    analyze_arp_table()

    # Get interfaces and generate target IPs
    get_interfaces()
    generate_ip_list()

    if subnet_ip_blocks.qsize() == 0:
        print("[!] No valid IPs to scan. Exiting.")
        sys.exit(0)

    # Start the ping scan using a thread pool
    ping_all_ips()

    # Stop monitoring threads
    stop_threads.set()

    print("[+] Scan complete.")

#End
