import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Order } from '../types/order';

interface OrderListProps {
  orders: Order[];
}

export const OrderList: React.FC<OrderListProps> = ({ orders }) => {
  const getStatusText = (status: any): string => {
    // Handle both string and numeric status
    if (typeof status === 'number') {
      return status === 0 ? 'Pending' : status === 1 ? 'Paid' : 'Failed';
    }
    return status;
  };

  const getStatusColor = (status: any) => {
    const statusText = getStatusText(status);
    switch (statusText) {
      case 'Paid':
        return 'text-green-600 bg-green-50';
      case 'Failed':
        return 'text-red-600 bg-red-50';
      case 'Pending':
      default:
        return 'text-yellow-600 bg-yellow-50';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  if (orders.length === 0) {
    return (
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Orders</CardTitle>
          <CardDescription>No orders found. Create your first order to get started.</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle>Orders</CardTitle>
        <CardDescription>View and manage your orders</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {orders.map((order, index) => (
            <div key={order.orderNumber || index} className="border rounded-lg p-4 space-y-2">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-semibold text-lg">{order.orderNumber}</h3>
                  <p className="text-sm text-muted-foreground">
                    Created: {formatDate(order.createdAt)}
                  </p>
                </div>
                <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(order.status)}`}>
                  {getStatusText(order.status)}
                </span>
              </div>
              
              <div className="flex justify-between items-center">
                <div className="text-lg font-semibold">
                  {order.currency} {order.amount.toFixed(2)}
                </div>
                <div className="text-sm text-muted-foreground">
                  ID: {typeof order.id === 'object' ? JSON.stringify(order.id) : order.id}
                </div>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
};
