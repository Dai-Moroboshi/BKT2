class WalletTransactionModel {
  final int id;
  final double amount;
  final String type;
  final String status;
  final String? relatedId;
  final String? description;
  final DateTime createdDate;

  WalletTransactionModel({
    required this.id,
    required this.amount,
    required this.type,
    required this.status,
    this.relatedId,
    this.description,
    required this.createdDate,
  });

  factory WalletTransactionModel.fromJson(Map<String, dynamic> json) {
    return WalletTransactionModel(
      id: json['id'] ?? 0,
      amount: (json['amount'] ?? 0).toDouble(),
      type: json['type'] ?? '',
      status: json['status'] ?? '',
      relatedId: json['relatedId'],
      description: json['description'],
      createdDate: json['createdDate'] != null
          ? DateTime.parse(json['createdDate'])
          : DateTime.now(),
    );
  }

  bool get isIncome => amount > 0;
  bool get isPending => status == 'Pending';
  bool get isCompleted => status == 'Completed';
}

class WalletSummaryModel {
  final double balance;
  final double totalDeposit;
  final double totalSpent;
  final double totalReward;
  final int pendingTransactions;

  WalletSummaryModel({
    required this.balance,
    required this.totalDeposit,
    required this.totalSpent,
    required this.totalReward,
    required this.pendingTransactions,
  });

  factory WalletSummaryModel.fromJson(Map<String, dynamic> json) {
    return WalletSummaryModel(
      balance: (json['balance'] ?? 0).toDouble(),
      totalDeposit: (json['totalDeposit'] ?? 0).toDouble(),
      totalSpent: (json['totalSpent'] ?? 0).toDouble(),
      totalReward: (json['totalReward'] ?? 0).toDouble(),
      pendingTransactions: json['pendingTransactions'] ?? 0,
    );
  }
}
