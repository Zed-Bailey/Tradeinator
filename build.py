import os
import subprocess

systemd_file_location = "/lib/systemd/system/"



# ========================================
# Update me with the projects you want to deploy
# ========================================
projects = [
    "Tradeinator.DataIngestion.Forex",
    "Tradeinator.Notifications",
    "Tradeinator.Strategy.MamaFama",
]
# ========================================


print(f"building the following projects:\n" + '\n'.join(projects))
continue_building = input("Continue [y/n]: ")
if continue_building != "y":
    print("exiting")
    exit(0)

if not os.environ.get("SUDO_UID"):
    print(f"[ERROR] this script requires being run with sudo permissions as it writes systemd service files to {systemd_file_location}")
    exit(1)






def build_project():
    return subprocess.run(["dotnet", "publish", "-c", "Release", "-o", "../build"], capture_output=True)
    


def get_unit_file(description, dotnet_path, build_dll_path):
    return f"""
    [Unit]
    Description={description} Service
    After=multi-user.target

    [Service]
    Type=idle
    ExecStart={dotnet_path} {build_dll_path}
    Environment=DOTNET_ROOT={dotnet_path}

    [Install]
    WantedBy=multi-user.target
    """


which_dotnet = subprocess.run(["/usr/bin/which", "dotnet"], capture_output=True)
if which_dotnet.returncode == 1:
    print("[ERROR] 'which dotnet' failed which means you dont have .net installed on your system")
    exit(1)

dotnet_path = which_dotnet.stdout.decode().strip()



curr_dir = os.getcwd()
if curr_dir.split('/')[-1] != "Tradeinator":
    print(f"[ERROR] Not in root Tradeinator directory in {curr_dir}")
    exit(1)

print(f"In root Tradeinator directory: {curr_dir}")

build_dir = curr_dir + '/build'
os.makedirs(build_dir, exist_ok=True)
print(f"Build will be output to: {curr_dir + '/build'}")




for project in projects:
    # move into directory
    os.chdir(project)
    print(f"building {os.getcwd()}")

    out = build_project()
    if out.returncode == -1:
        print(f"[ERROR] failed to build project {os.getcwd()}\n{out.stdout.decode()}")
        exit(1)

    print("built")
    # move back up
    os.chdir("..")



if not os.path.exists(systemd_file_location):
    print(f"[ERROR] systemd path service path does not exist : {systemd_file_location}")
    exit(1)

unit_files = []


for project in projects:
    unit_file = get_unit_file(project, dotnet_path, f"{build_dir}/{project}.dll")
    unit_file_name = project.lower().replace('.', '-') + ".service"
    unit_files.append(unit_file_name)
    f = open(f"{systemd_file_location}{unit_file_name}", 'w')
    f.write(unit_file)
    f.close()
    print(f"wrote systemd service file to: {systemd_file_location}{unit_file_name}")


print("services already running? run:")
for file in unit_files:
    print(f"sudo systemctl disable {file}")

print("#################")

print("reload daemon (required):\nsudo systemctl daemon-reload")

print("enable services (to start on boot):")
for file in unit_files:
    print(f"sudo systemctl enable {file}")
print("sudo reboot")

print("#################")

print("to start services now (does not enable them to start on boot):")
for file in unit_files:
    print(f"sudo systemctl start {file}")


print("#################")



