# Virtual Machines

Manage your Azure VMs without leaving Visual Studio.

## Actions

| Action | Description |
|--------|-------------|
| **Start / Stop / Restart** | Control VM power state with one click |
| **Connect via RDP** | Launch Remote Desktop for Windows VMs |
| **Connect via SSH** | Open SSH connections for Linux VMs |
| **Copy IP Address** | Quick access to public IP for scripts or tools |
| **View Status** | See running, stopped, or deallocated state at a glance |
| **Open in Portal** | Jump to full VM management in Azure Portal |
| **Add Tags** | Organize with resource tags |

## VM States

| State | Description | Billing |
|-------|-------------|---------|
| **Running** | VM is on and operational | Charged |
| **Stopped** | OS shut down, but VM allocated | Charged |
| **Deallocated** | VM resources released | Not charged (except storage) |

**Tip:** Use **Stop (Deallocate)** from Azure Explorer to stop billing. Just shutting down from within the OS leaves the VM in "Stopped" state, which still incurs charges.

## Connecting to VMs

### RDP (Windows VMs)

1. Ensure the VM is **Running**
2. Right-click → **Connect via RDP**
3. Windows Remote Desktop Connection opens with the VM's IP pre-filled
4. Enter your VM credentials when prompted

**Requirements:**
- VM must have a public IP address (or you need VPN/ExpressRoute access)
- NSG must allow inbound port **3389**
- Windows Remote Desktop client (built into Windows)

### SSH (Linux VMs)

1. Ensure the VM is **Running**
2. Right-click → **Connect via SSH**
3. A terminal opens with the SSH command ready
4. Enter your credentials or use your SSH key

**Requirements:**
- VM must have a public IP address
- NSG must allow inbound port **22**
- SSH client (OpenSSH is built into Windows 10+)

## Required Permissions

| Action | Minimum Role |
|--------|--------------|
| View VM | Reader |
| Start / Stop / Restart | Virtual Machine Contributor |
| Connect (RDP/SSH) | Reader (just copies IP; actual VM access needs VM credentials) |
| Modify VM settings | Virtual Machine Contributor |

## Troubleshooting

### RDP connection fails

1. **VM not running** — Start the VM first
2. **No public IP** — Check if the VM has a public IP assigned
3. **NSG blocking** — Verify inbound rule allows TCP port 3389
4. **Windows Firewall** — The VM's Windows Firewall may block RDP
5. **NLA issues** — Try disabling Network Level Authentication temporarily in Azure Portal

### SSH connection fails

1. **VM not running** — Start the VM first
2. **No public IP** — Check if the VM has a public IP assigned
3. **NSG blocking** — Verify inbound rule allows TCP port 22
4. **Wrong credentials** — Verify username and SSH key/password
5. **SSH service** — Ensure SSH daemon is running on the VM

### Can't start the VM

1. **Quota exceeded** — Your subscription may have hit vCPU quota limits
2. **Resource locks** — Check for delete/read-only locks on the VM or resource group
3. **Disk issues** — The OS disk may have problems; check Boot diagnostics in Portal
4. **Region capacity** — Rarely, Azure regions run low on specific VM sizes

### VM stuck in "Updating" state

1. Wait a few minutes — some operations take time
2. Check **Activity Log** in Azure Portal for errors
3. If stuck for more than 15 minutes, try the Azure Portal to force operations

## Azure Documentation

- [Virtual Machines overview](https://learn.microsoft.com/en-us/azure/virtual-machines/overview)
- [Connect to Windows VM](https://learn.microsoft.com/en-us/azure/virtual-machines/windows/connect-rdp)
- [Connect to Linux VM](https://learn.microsoft.com/en-us/azure/virtual-machines/linux/ssh-from-windows)
- [VM sizes](https://learn.microsoft.com/en-us/azure/virtual-machines/sizes)
- [Troubleshoot RDP](https://learn.microsoft.com/en-us/troubleshoot/azure/virtual-machines/windows/troubleshoot-rdp-connection)
- [Troubleshoot SSH](https://learn.microsoft.com/en-us/troubleshoot/azure/virtual-machines/linux/troubleshoot-ssh-connection)

## See Also

- [Resource Tags](../features/tags.md)
- [Troubleshooting](../troubleshooting.md)
