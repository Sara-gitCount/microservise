# Build and push script for Azure Container Registry or Docker Hub
# PowerShell version

param(
    [Parameter(Mandatory=$true)]
    [string]$Registry,
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest"
)

Write-Host "Building and pushing images to $Registry with tag $ImageTag"

$services = @(
    @{ name = "auth-service"; dockerfile = "Services/AuthService/Dockerfile" },
    @{ name = "catalog-service"; dockerfile = "Services/CatalogService/Dockerfile" },
    @{ name = "order-service"; dockerfile = "Services/OrderService/Dockerfile" },
    @{ name = "lottery-service"; dockerfile = "Services/LotteryService/Dockerfile" },
    @{ name = "api-gateway"; dockerfile = "Gateway/ApiGateway/Dockerfile" }
)

foreach ($service in $services) {
    $imageName = "$Registry/mechira-$($service.name):$ImageTag"
    Write-Host "`nBuilding $imageName..."
    
    docker build -f $service.dockerfile -t $imageName .
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Pushing $imageName..."
        docker push $imageName
    }
    else {
        Write-Host "Failed to build $($service.name)" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`nAll images pushed successfully!" -ForegroundColor Green
Write-Host "Images available at:"
$services | ForEach-Object {
    Write-Host "  - $Registry/mechira-$($_.name):$ImageTag"
}
