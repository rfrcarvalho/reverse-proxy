# Releasing YARP

This document provides a guide on how to release a preview of YARP.

To keep track of the process, open a [release checklist issue](https://github.com/microsoft/reverse-proxy/issues/new?title=Preview%20X%20release%20checklist&body=See%20%5BRelease.md%5D%28https%3A%2F%2Fgithub.com%2Fmicrosoft%2Freverse-proxy%2Fblob%2Fmain%2Fdocs%2Foperations%2FRelease.md%29%20for%20detailed%20instructions.%0A%0A-%20%5B%20%5D%20Ensure%20there%27s%20a%20release%20branch%20created%20%28see%20%5BBranching%5D%28https%3A%2F%2Fgithub.com%2Fmicrosoft%2Freverse-proxy%2Fblob%2Fmain%2Fdocs%2Foperations%2FBranching.md%29%29%0A-%20%5B%20%5D%20Ensure%20the%20%60Version.props%60%20has%20the%20%60PreReleaseVersionLabel%60%20updated%20to%20the%20next%20preview%0A-%20%5B%20%5D%20Identify%20and%20validate%20the%20build%20on%20the%20%60microsoft-reverse-proxy-official%60%20pipeline%0A-%20%5B%20%5D%20Release%20the%20build%0A-%20%5B%20%5D%20Tag%20the%20commit%0A-%20%5B%20%5D%20Draft%20release%20notes%0A-%20%5B%20%5D%20Update%20samples%20to%20use%20updated%20package%20versions%0A-%20%5B%20%5D%20Publish%20the%20docs%0A-%20%5B%20%5D%20Publish%20release%20notes%0A-%20%5B%20%5D%20Close%20the%20%5Bold%20milestone%5D%28https%3A%2F%2Fgithub.com%2Fmicrosoft%2Freverse-proxy%2Fmilestones%29%0A-%20%5B%20%5D%20Announce%20on%20social%20media%0A-%20%5B%20%5D%20Set%20the%20preview%20branch%20to%20protected%0A-%20%5B%20%5D%20Delete%20the%20%5Bprevious%20preview%20branch%5D%28https%3A%2F%2Fgithub.com%2Fmicrosoft%2Freverse-proxy%2Fbranches%29%0A-%20%5B%20%5D%20Request%20source%20code%20archival).

## Versioning

Ensure the eng/Versions.props file has the expected versions and pre-release labels. For a final release set PreReleaseVersionLabel to `rtw`.

## Ensure there's a release branch created.

See [Branching](Branching.md):
- Make the next preview branch.
- Update the branding in main.
- Update the global.json runtime and SDK versions in main.

## Identify the Final Build

First, identify the final build of the [`microsoft-reverse-proxy-official` Azure Pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=809&_a=summary) (on dnceng/internal). The final build will be the latest successful build **in the relevant `release/x` branch**. Use the "Branches" tab on Azure DevOps to help identify it. If the branch hasn't been mirrored yet (see [code-mirror pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=16&keywordFilter=microsoft%20reverse-proxy)) and there are no outstanding changesets in the branch, the build of the corresponding commit from the main branch can be used.

Once you've identified that build, click in to the build details.

## Validate the Final Build

At this point, you can perform any validation that makes sense. At a minimum, we should validate that the sample can run with the candidate packages. You can download the final build using the "Artifacts" which can be accessed under "Related" in the header:

![image](https://user-images.githubusercontent.com/7574/81447119-e4204800-9130-11ea-8952-9a0f9831f678.png)

The packages can be accessed from the `PackageArtifacts` artifact:

![image](https://user-images.githubusercontent.com/7574/81447168-fef2bc80-9130-11ea-8aa0-5a83d90efa0d.png)

### Consume .nupkg
- Visual Studio: Place it in a local folder and add that folder as a nuget feed in Visual Studio.
- Command Line: `dotnet nuget add source <directory> -n local`

Walk through the [Getting Started](https://microsoft.github.io/reverse-proxy/articles/getting_started.html) instructions and update them in the release branch as needed.

Also validate any major new scenarios this release and their associated docs.

## Release the build

Once validation has been completed, it's time to release. Go back to the Final Build in Azure DevOps. It's probably good to triple-check the version numbers of the packages in the artifacts against whatever validation was done at this point.

Select "Release" from the triple-dot menu in the top-right of the build details page:

![image](https://user-images.githubusercontent.com/7574/81447354-55f89180-9131-11ea-84bc-0138d7b211e4.png)

Verify the Release Pipeline selected is `microsoft-reverse-proxy-release`, that the `NuGet.org` stage has a blue border (meaning it will automatically deploy) and that the build number under Artifacts matches the build number of the final build (it will not match the package version). The defaults selected by Azure Pipelines should configure everything correctly but it's a good idea to double check.

![image](https://user-images.githubusercontent.com/7574/81447433-76c0e700-9131-11ea-9e8b-e4984ab7c31a.png)

Click "Create" to start the release! Unless you're a release approver, you're done here!

## Approve the release

The Azure Pipeline will send an email to all the release approvers asking one of them to approve the release:

![image](https://user-images.githubusercontent.com/7574/81447680-f3ec5c00-9131-11ea-821c-37dbe467faee.png)

Click "View Approval", or navigate to the release directly in Azure DevOps. You'll see that the stage is "Pending Approval"

![image](https://user-images.githubusercontent.com/7574/81447753-10889400-9132-11ea-9dd2-26b2f6bc8970.png)

Click "Approve" to bring up the Approval dialog. Enter a comment such as "release for preview X" and click "Approve" to finalize the release. **After pressing "Approve", packages will be published automatically**. It *is* possible to cancel the pipeline, but it might be too late. See "Troubleshooting" below.

![image](https://user-images.githubusercontent.com/7574/81447898-4d548b00-9132-11ea-89df-b4624a5e037d.png)

Click "Reject" to cancel the release.

The packages will be pushed and when the "NuGet.org" stage turns green, the packages are published!

*Note: NuGet publishing is quick, but there is a background indexing process that can mean it will take up to several hours for packages to become available*

## Tag the commit

Create and push a git tag for the commit associated with the final build (not necessarily the HEAD of the current release branch). See prior tags for the preferred format. Use a lightweight tag, not annotated.

`git tag v1.0.0-previewX`

Push the tag change to the upstream repo (**not your fork**)

`git push upstream --tags`

## Draft release notes

Create a draft release at https://github.com/microsoft/reverse-proxy/releases using the new tag. See prior releases for the recommended content and format.

## Update samples to use updated package version

Projects in the **samples** folder use `PackageReference` on the release branch and `ProjectReference` on the main branch.

Update all project files in the samples folder on the release branch to use `PackageReference` with the newly-released version. Make sure the samples are buildable after this change.

## Publish the docs

Reset the `release/latest` branch to the head of the current preview branch to publish the latest docs. See [docs](../docfx/readme.md).

## Publish the release notes

Publish the draft release notes. These should be referencing the latest docs, packages, etc..

## Close the old milestone

It should be empty now. If it's not, move the outstanding issues to the next one.

## Announce on social media

David Fowler has a lot of twitter followers interested in YARP. Tweet a link to the release notes and let him retweet it.

## Set the preview branch to protected

This is to avoid accidental pushes to/deletions of the preview branch.

## Delete the previous preview branch

There should only be one [preview branch on the repo](https://github.com/microsoft/reverse-proxy/branches) after this point.

## Request source code archival
1. Go to the internal https://aka.ms/dpsrequestforms portal
2. Switch to "Source Code Archival Request Page"
3. Proceed through steps 1,2 till the step 3 (all prerequisites are already fulfilled)
4. Fill in the required fields on the steps 3, 4, 5. Please, see the recommended values below.
5. At the last step, check all the info and and submit the request
6. Go to "My Request" tab and wait for a new ticket to appear on the list
7. Copy the ticket link from "Ticket ID" column which will look like `https://prod.******`
8. Replace the `prod` word to `portal`
9. Navigate to the fixed link and check that the ticket is actually created
10. That's all the actions needed to be done immediately. Afterwards, periodically track the ticket progress. It might take many hours.
11. [Offline] Wait for the archival completion report to arrive. Check that the size and number of archived files match the YARP repo.

### Recommended fields' values for archival request form
| Field | Value |
| --- | --- |
| Team Alias | dotnetrp |
| Business Group Name | Devdiv |
| Product Name | YARP |
| Version | \<release version\> |
| Production Type | dotNET |
| Release Type | \<RC or Release\> |
| Operating System(s) | Cross Platform |
| Product Language(s) | English |
| Release Date | \<release date\> |
| File Count | \<rough number of files in repo\> |
| Back Up Type | Code Repo(Git URL/AzureDevOps) |
| Repo URL | \<link to the internal AzDo YARP repo\> |
| OwnerAlias | dotnetrp |
| File Collection | Build Scripts, Help Utility Source Code, Source Code |
| Data Size | \<rough total files size in MB\> |

## Troubleshooting

### Authentication Errors

The pipeline is authenticated via a "Service Connection" in Azure DevOps. If there are authentication errors, it's likely the API key is invalid. Follow these steps to update the API key:

1. Go to NuGet.org, log in with an account associated with an `@microsoft.com` address that has access to the `dotnetframework` organization.
2. Generate a new API key with "dotnetframework" as the Package Owner and "*" as the Package "glob".
3. Copy that API key and fill it in to the "nuget.org (dotnetframework organization)" [Service Connection](https://dev.azure.com/dnceng/internal/_settings/adminservices) in Azure DevOps.

In the event you don't have access, contact `dnceng@microsoft.com` for guidance.

### Accidental Overpublish

In the event you overpublish (publish a package that wasn't intended to be released), you should "unlist" the package on NuGet. It is not possible to delete packages on NuGet.org, by design, but you can remove them from search results. Users who reference the version you published directly will still be able to download it, but it won't show up in search queries or non-version-specific actions (like installing the latest).

1. Go to NuGet.org, log in with an account associated with an `@microsoft.com` address that has access to the `dotnetframework` organization.
2. Go to the package page and click "Manage package" on the "Info" sidebar on the right.
3. Expand "Listing"
4. Select the version that was accidentally published
5. Uncheck the "List in search results" box
6. Click "Save"

### Package was rejected

NuGet.org has special criteria for all packages starting `Microsoft.`. If the package is rejected for not meeting one of those criteria, go to the [NuGet @ Microsoft](http://aka.ms/nuget) page for more information on required criteria and guidance for how to configure the package appropriately.
