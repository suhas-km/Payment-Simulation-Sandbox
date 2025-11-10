# Ecommerce Frontend

A modern React frontend for the Ecommerce API built with TypeScript, Tailwind CSS, and shadcn/ui components.

## Features

- Create new orders with order number, amount, and currency
- View list of created orders with status indicators
- Modern, responsive UI with Tailwind CSS
- Type-safe API communication with TypeScript
- Component-based architecture with shadcn/ui

## Getting Started

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn

### Installation

1. Install dependencies:
```bash
npm install
```

2. Start the development server:
```bash
npm run dev
```

The frontend will be available at `http://localhost:3000`.

### API Configuration

The frontend is configured to proxy API requests to the C# backend running on `http://localhost:5000`. Make sure your Ecommerce API is running on that port.

## Project Structure

```
src/
├── components/
│   ├── ui/           # shadcn/ui components
│   ├── CreateOrderForm.tsx
│   └── OrderList.tsx
├── services/
│   └── api.ts        # API service layer
├── types/
│   └── order.ts      # TypeScript type definitions
├── lib/
│   └── utils.ts      # Utility functions
├── App.tsx           # Main application component
├── main.tsx          # Application entry point
└── index.css         # Global styles
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## API Integration

The frontend communicates with the C# API through the following endpoints:

- `POST /api/orders` - Create a new order
- `GET /api/orders` - List all orders (not yet implemented in backend)
- `GET /api/orders/{orderNumber}` - Get specific order (not yet implemented in backend)

Note: The GET endpoints would need to be added to your C# API for full functionality.
