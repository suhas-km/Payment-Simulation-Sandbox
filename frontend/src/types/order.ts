export type OrderStatus = 'Pending' | 'Paid' | 'Failed';

export interface Order {
  id: string;
  orderNumber: string;
  amount: number;
  currency: string;
  status: OrderStatus;
  createdAt: string;
}

export interface CreateOrderRequest {
  orderNumber: string;
  amount: number;
  currency?: string;
}
