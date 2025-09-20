# Use official Node.js LTS image
FROM node:20-alpine

# Set working directory
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm ci --omit=dev

# Copy the rest of the app
COPY . .

# Build Prisma client
RUN npx prisma generate

# Expose port
EXPOSE 3001

# Start the app
CMD ["npm", "start"]
