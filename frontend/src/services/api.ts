import { CreateOrderRequest, Order } from '../types/order';

const API_BASE_URL = '/api';

class ApiService {
  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    });

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`);
    }

    return response.json() as Promise<T>;
  }

  async createOrder(order: CreateOrderRequest): Promise<Order> {
    // Generate a simple idempotency key
    const idempotencyKey = `order-${order.orderNumber}-${Date.now()}`;
    
    return this.request<Order>('/orders', {
      method: 'POST',
      headers: {
        'Idempotency-Key': idempotencyKey,
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      body: JSON.stringify(order),
    });
  }

  async getOrders(): Promise<Order[]> {
    // Note: This endpoint would need to be added to your C# API
    return this.request<Order[]>('/orders');
  }

  async getOrder(orderNumber: string): Promise<Order> {
    // Note: This endpoint would need to be added to your C# API
    return this.request<Order>(`/orders/${orderNumber}`);
  }
}

export const apiService = new ApiService();
