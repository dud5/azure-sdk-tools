﻿# ----------------------------------------------------------------------------------
#
# Copyright Microsoft Corporation
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# ----------------------------------------------------------------------------------

########################################################################### General Store Scenario Tests ###########################################################################

<#
.SYNOPSIS
Tests any cloud based cmdlet with invalid credentials and expect it'll throw an exception.
#>
function Test-WithInvalidCredentials
{
	param([ScriptBlock] $cloudCmdlet)
	
	# Setup
	Remove-AllSubscriptions

	# Test
	Assert-Throws $cloudCmdlet "Call Set-AzureSubscription and Select-AzureSubscription first."
}

<#
.SYNOPSIS
Tests Get-AzureStoreAvailableAddOn with invalid credentials and make sure it works.
#>
function Test-WithInvalidCredentialsWorks
{
	# Setup
	Remove-AllSubscriptions

	# Test
	Test-GetAzureStoreAvailableAddOnWithDefaultCountry
}

########################################################################### Get-AzureStoreAvailableAddOn Scenario Tests ###########################################################################

<#
.SYNOPSIS
Tests using Get-AzureStoreAvailableAddOn with default country (US)
#>
function Test-GetAzureStoreAvailableAddOnWithDefaultCountry
{
	# Test
	$actual = Get-AzureStoreAvailableAddOn

	# Assert
	Assert-True { $actual.Count -gt 0 }
	$actual | % { Assert-NotNull $_.Provider; Assert-NotNull $_.AddOn; Assert-NotNull $_.Plans }
}

<#
.SYNOPSIS
Tests using Get-AzureStoreAvailableAddOn with specified country that will not return any addons.
#>
function Test-GetAzureStoreAvailableAddOnWithNoAddOns
{
	# Test
	$actual = Get-AzureStoreAvailableAddOn "E1"

	# Assert
	Assert-True { $actual.Count -eq 0 }
}

<#
.SYNOPSIS
Tests using Get-AzureStoreAvailableAddOn with invalid country name.
#>
function Test-GetAzureStoreAvailableAddOnWithInvalidCountryName
{
	# Test
	Assert-Throws { Get-AzureStoreAvailableAddOn "UnitedStates" } "Cannot validate argument on parameter 'Country'. The country name is invalid, please use a valid two character country code, as described in ISO 3166-1 alpha-2."
}