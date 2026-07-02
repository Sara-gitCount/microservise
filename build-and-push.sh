#!/bin/bash
# Docker build and push script for Azure Container Registry or Docker Hub

set -e

REGISTRY=${1:-""}
IMAGE_TAG=${2:-"latest"}

if [ -z "$REGISTRY" ]; then
    echo "Usage: ./build-and-push.sh <registry-url> [image-tag]"
    echo "Example: ./build-and-push.sh myregistry.azurecr.io latest"
    exit 1
fi

echo "Building and pushing images to $REGISTRY with tag $IMAGE_TAG"

# Build and push each service
docker build -f Services/AuthService/Dockerfile -t "$REGISTRY/mechira-auth-service:$IMAGE_TAG" .
docker push "$REGISTRY/mechira-auth-service:$IMAGE_TAG"

docker build -f Services/CatalogService/Dockerfile -t "$REGISTRY/mechira-catalog-service:$IMAGE_TAG" .
docker push "$REGISTRY/mechira-catalog-service:$IMAGE_TAG"

docker build -f Services/OrderService/Dockerfile -t "$REGISTRY/mechira-order-service:$IMAGE_TAG" .
docker push "$REGISTRY/mechira-order-service:$IMAGE_TAG"

docker build -f Services/LotteryService/Dockerfile -t "$REGISTRY/mechira-lottery-service:$IMAGE_TAG" .
docker push "$REGISTRY/mechira-lottery-service:$IMAGE_TAG"

docker build -f Gateway/ApiGateway/Dockerfile -t "$REGISTRY/mechira-api-gateway:$IMAGE_TAG" .
docker push "$REGISTRY/mechira-api-gateway:$IMAGE_TAG"

echo "All images pushed successfully!"
echo "Images available at:"
echo "  - $REGISTRY/mechira-auth-service:$IMAGE_TAG"
echo "  - $REGISTRY/mechira-catalog-service:$IMAGE_TAG"
echo "  - $REGISTRY/mechira-order-service:$IMAGE_TAG"
echo "  - $REGISTRY/mechira-lottery-service:$IMAGE_TAG"
echo "  - $REGISTRY/mechira-api-gateway:$IMAGE_TAG"
