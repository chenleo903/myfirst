import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Space,
  Spin,
  Timeline,
  Typography,
  message,
  Popconfirm,
  Empty,
  Modal,
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  ArrowLeftOutlined,
  PlusOutlined,
  PhoneOutlined,
  WechatOutlined,
  MailOutlined,
  TeamOutlined,
  EnvironmentOutlined,
} from '@ant-design/icons';
import { useCustomer, useDeleteCustomer } from '../hooks/useCustomers';
import { useInteractions, useDeleteInteraction } from '../hooks/useInteractions';
import { InteractionForm } from '../components';
import type { CustomerStatus, CustomerSource, InteractionChannel, Interaction } from '../types';

const { Title, Text, Paragraph } = Typography;

// Status display configuration
const statusConfig: Record<CustomerStatus, { color: string; label: string }> = {
  Lead: { color: 'blue', label: '线索' },
  Contacted: { color: 'cyan', label: '已联系' },
  NeedsAnalyzed: { color: 'orange', label: '待分析' },
  Quoted: { color: 'purple', label: '已报价' },
  Negotiating: { color: 'gold', label: '谈判中' },
  Won: { color: 'green', label: '成交' },
  Lost: { color: 'red', label: '流失' },
};

// Source display configuration
const sourceConfig: Record<CustomerSource, string> = {
  Website: '网站',
  Referral: '推荐',
  SocialMedia: '社交媒体',
  Event: '活动',
  DirectContact: '直接联系',
  Other: '其他',
};

// Channel display configuration
const channelConfig: Record<InteractionChannel, { icon: React.ReactNode; label: string }> = {
  Phone: { icon: <PhoneOutlined />, label: '电话' },
  Wechat: { icon: <WechatOutlined />, label: '微信' },
  Email: { icon: <MailOutlined />, label: '邮件' },
  Offline: { icon: <TeamOutlined />, label: '线下' },
  Other: { icon: <EnvironmentOutlined />, label: '其他' },
};


export default function CustomerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [interactionModalOpen, setInteractionModalOpen] = useState(false);
  const [editingInteraction, setEditingInteraction] = useState<Interaction | null>(null);

  const { data: customerData, isLoading: customerLoading, error: customerError } = useCustomer(id || '');
  const { data: interactionsData, isLoading: interactionsLoading } = useInteractions(id || '');
  const deleteCustomer = useDeleteCustomer();
  const deleteInteraction = useDeleteInteraction();

  const customer = customerData?.data?.data;
  const etag = customerData?.etag;
  const interactions = interactionsData?.data || [];

  const handleDeleteCustomer = async () => {
    if (!id) return;
    try {
      await deleteCustomer.mutateAsync({ id, etag });
      message.success('客户已删除');
      navigate('/customers');
    } catch {
      message.error('删除失败');
    }
  };

  const handleDeleteInteraction = async (interactionId: string) => {
    if (!id) return;
    try {
      await deleteInteraction.mutateAsync({ id: interactionId, customerId: id });
      message.success('互动记录已删除');
    } catch {
      message.error('删除失败');
    }
  };

  const handleAddInteraction = () => {
    setEditingInteraction(null);
    setInteractionModalOpen(true);
  };

  const handleEditInteraction = (interaction: Interaction) => {
    setEditingInteraction(interaction);
    setInteractionModalOpen(true);
  };

  const handleInteractionModalClose = () => {
    setInteractionModalOpen(false);
    setEditingInteraction(null);
  };

  if (customerLoading) {
    return (
      <div style={{ padding: '24px', textAlign: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (customerError || !customer) {
    return (
      <div style={{ padding: '24px' }}>
        <Card>
          <Empty description="客户不存在或已被删除" />
          <div style={{ textAlign: 'center', marginTop: 16 }}>
            <Button onClick={() => navigate('/customers')}>返回列表</Button>
          </div>
        </Card>
      </div>
    );
  }


  return (
    <div style={{ padding: '24px' }}>
      {/* Header */}
      <div style={{ marginBottom: 16 }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/customers')}
          style={{ marginBottom: 16 }}
        >
          返回列表
        </Button>
      </div>

      {/* Customer Info Card */}
      <Card
        title={
          <Space>
            <Title level={4} style={{ margin: 0 }}>
              {customer.companyName}
            </Title>
            <Tag color={statusConfig[customer.status]?.color}>
              {statusConfig[customer.status]?.label || customer.status}
            </Tag>
          </Space>
        }
        extra={
          <Space>
            <Button
              type="primary"
              icon={<EditOutlined />}
              onClick={() => navigate(`/customers/${id}/edit`)}
            >
              编辑
            </Button>
            <Popconfirm
              title="确定要删除此客户吗？"
              description="删除后客户数据将被标记为已删除"
              onConfirm={handleDeleteCustomer}
              okText="确定"
              cancelText="取消"
            >
              <Button danger icon={<DeleteOutlined />}>
                删除
              </Button>
            </Popconfirm>
          </Space>
        }
        style={{ marginBottom: 24 }}
      >
        <Descriptions column={{ xs: 1, sm: 2, md: 3 }} bordered>
          <Descriptions.Item label="联系人">{customer.contactName}</Descriptions.Item>
          <Descriptions.Item label="电话">{customer.phone || '-'}</Descriptions.Item>
          <Descriptions.Item label="邮箱">{customer.email || '-'}</Descriptions.Item>
          <Descriptions.Item label="微信">{customer.wechat || '-'}</Descriptions.Item>
          <Descriptions.Item label="行业">{customer.industry || '-'}</Descriptions.Item>
          <Descriptions.Item label="来源">
            {customer.source ? sourceConfig[customer.source] || customer.source : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="评分">{customer.score}</Descriptions.Item>
          <Descriptions.Item label="标签">
            {customer.tags?.length ? (
              <Space wrap>
                {customer.tags.map((tag, index) => (
                  <Tag key={index}>{tag}</Tag>
                ))}
              </Space>
            ) : (
              '-'
            )}
          </Descriptions.Item>
          <Descriptions.Item label="最后互动">
            {customer.lastInteractionAt
              ? new Date(customer.lastInteractionAt).toLocaleString('zh-CN')
              : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="创建时间">
            {new Date(customer.createdAt).toLocaleString('zh-CN')}
          </Descriptions.Item>
          <Descriptions.Item label="更新时间">
            {new Date(customer.updatedAt).toLocaleString('zh-CN')}
          </Descriptions.Item>
        </Descriptions>
      </Card>


      {/* Interaction Timeline */}
      <Card
        title="互动时间线"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleAddInteraction}>
            添加互动
          </Button>
        }
      >
        {interactionsLoading ? (
          <div style={{ textAlign: 'center', padding: 40 }}>
            <Spin />
          </div>
        ) : interactions.length === 0 ? (
          <Empty description="暂无互动记录" />
        ) : (
          <Timeline
            mode="left"
            items={interactions.map((interaction) => ({
              key: interaction.id,
              label: (
                <Text type="secondary">
                  {new Date(interaction.happenedAt).toLocaleString('zh-CN')}
                </Text>
              ),
              children: (
                <Card
                  size="small"
                  title={
                    <Space>
                      {channelConfig[interaction.channel]?.icon}
                      <span>{interaction.title}</span>
                      {interaction.stage && (
                        <Tag color={statusConfig[interaction.stage]?.color}>
                          {statusConfig[interaction.stage]?.label}
                        </Tag>
                      )}
                    </Space>
                  }
                  extra={
                    <Space size="small">
                      <Button
                        type="link"
                        size="small"
                        icon={<EditOutlined />}
                        onClick={() => handleEditInteraction(interaction)}
                      />
                      <Popconfirm
                        title="确定要删除此互动记录吗？"
                        onConfirm={() => handleDeleteInteraction(interaction.id)}
                        okText="确定"
                        cancelText="取消"
                      >
                        <Button type="link" size="small" danger icon={<DeleteOutlined />} />
                      </Popconfirm>
                    </Space>
                  }
                >
                  {interaction.summary && (
                    <Paragraph ellipsis={{ rows: 2, expandable: true }}>
                      {interaction.summary}
                    </Paragraph>
                  )}
                  {interaction.nextAction && (
                    <div style={{ marginTop: 8 }}>
                      <Text type="secondary">下一步：</Text>
                      <Text>{interaction.nextAction}</Text>
                    </div>
                  )}
                  {interaction.attachments && interaction.attachments.length > 0 && (
                    <div style={{ marginTop: 8 }}>
                      <Text type="secondary">附件：</Text>
                      <Space wrap>
                        {interaction.attachments.map((att, idx) => (
                          <a key={idx} href={att.url} target="_blank" rel="noopener noreferrer">
                            {att.fileName || `附件${idx + 1}`}
                          </a>
                        ))}
                      </Space>
                    </div>
                  )}
                </Card>
              ),
            }))}
          />
        )}
      </Card>

      {/* Interaction Form Modal */}
      <Modal
        title={editingInteraction ? '编辑互动记录' : '添加互动记录'}
        open={interactionModalOpen}
        onCancel={handleInteractionModalClose}
        footer={null}
        width={600}
        destroyOnClose
      >
        <InteractionForm
          customerId={id || ''}
          interaction={editingInteraction}
          onSuccess={handleInteractionModalClose}
          onCancel={handleInteractionModalClose}
        />
      </Modal>
    </div>
  );
}
