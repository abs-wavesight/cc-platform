variable "tenant_id" { 
  type = string 
}
variable "subscription_id" { 
  type = string 
}
variable "client_id" { 
  type = string 
}
variable "client_secret" { 
  type = string
  sensitive = true 
}
variable "build_resource_group_name" { 
  type = string 
}
variable "sku" { 
  type = string

  validation {
    condition = contains(["2019-Datacenter", "2022-Datacenter"], var.sku)
    error_message = "Invalid SKU"
  }
}
variable "vm_size" { 
  type = string

  validation {
    condition = contains(["Standard_D1_v2", "Standard_D2_v2", "Standard_D3_v2"], var.vm_size)
    error_message = "Invalid VM Size"
  }
}
variable "image_name" { 
  type = string 
}
variable "image_resource_group_name" { 
  type = string 
}
variable "site_system_release_version" { 
  type = string 
}
variable "created_timestamp" { 
  type = string 
}

locals {
  provision_path = "C:/ABS/provision"
}

source "azure-arm" "cc-site-system" {
  azure_tags = {
    app = "commoncore"
    ccservice = "cp"
    cccpnodetype = "site"
    cccpsystemreleaseversion = var.site_system_release_version
    imagecreated = var.created_timestamp
  }
  build_resource_group_name         = var.build_resource_group_name
  client_id                         = var.client_id
  client_secret                     = var.client_secret
  communicator                      = "winrm"
  image_offer                       = "WindowsServer"
  image_publisher                   = "MicrosoftWindowsServer"
  image_sku                         = var.sku
  managed_image_name                = var.image_name
  managed_image_resource_group_name = var.image_resource_group_name
  os_type                           = "Windows"
  subscription_id                   = var.subscription_id
  tenant_id                         = var.tenant_id
  vm_size                           = var.vm_size
  winrm_insecure                    = true
  winrm_timeout                     = "5m"
  winrm_use_ssl                     = true
  winrm_username                    = "packer"
}

build {
  sources = ["source.azure-arm.cc-site-system"]

  provisioner "powershell" {
    inline = [
      "New-Item -ItemType Directory -Force -Path ${var.provision_path}/",
    ]
  }

  provisioner "file" {
    source      = "to-copy/"
    destination = "${var.provision_path}/"
  }

  provisioner "powershell" {
    script      = "scripts/provision-site.ps1"
    remote_path = "${var.provision_path}/provision-site.ps1"
    elevated_user = "Administrator" // TODO RH: Should this be "SYSTEM"/"" instead so it runs as a service account?
    elevated_password = build.Password
    environment_vars = [
      "PROVISION_PATH=${var.provision_path}",
      "ABS_PATH=C:/ABS"
    ]
  }

}